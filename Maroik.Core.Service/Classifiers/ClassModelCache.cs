using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Classifiers;

/// <summary>
/// The class-suggestion models the website read from the file storage, kept in memory (a singleton) so the form's frequent questions
/// (one on every pause in typing) do not download and load a model each time. A model is kept while it is used: one unused for the
/// session time (<see cref="ServerSetting.SessionExpireMinutes"/>, the session's own idle timeout) leaves, like the session it served, at
/// the next sweep (every <see cref="_sweepInterval"/>). The session's end itself cannot be watched (a closed
/// browser tells the server nothing; the sessions live in Valkey), so the model's own idle time stands for it. Whatever the number of
/// users, at most <c>capacity</c> accounts' books are kept (the least recently used leaves first). A model that left is read again
/// from its file when needed. It also remembers which training it already asked the worker for, so one is asked for once per change
/// of records rather than on every question — and again after <see cref="TrainingRequestRetryAfter"/> if no model came (a lost message).
/// </summary>
public sealed class ClassModelCache : IDisposable
{
    /// <summary>
    /// The default number of accounts' books kept. Measured 2026-11-14 on 8,649 records of 40 classes (the size of the owner's
    /// expenditures): the stored file is 655 KB, the loaded model about 6.5 MB and reading it about 20 ms, so the cache stays near
    /// 330 MB in the worst case; a usual account's model is far smaller.
    /// </summary>
    public const int DefaultCapacity = 50;

    /// <summary>How long a training request is not repeated for the same records.</summary>
    public static readonly TimeSpan TrainingRequestRetryAfter = TimeSpan.FromMinutes(10);

    /// <summary>How often the models unused for the session time are dropped.</summary>
    private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(1);

    /// <summary>The clock of the idle times and the training requests.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>How long a model is kept unused: the session's idle timeout.</summary>
    private readonly TimeSpan _idleTimeout;

    /// <summary>How many accounts' books are kept at most.</summary>
    private readonly int _capacity;

    /// <summary>Runs <see cref="Sweep"/> every <see cref="_sweepInterval"/>.</summary>
    private readonly ITimer _sweep;

    /// <summary>A cache that keeps a model for the session time of <paramref name="settings"/>, at most <paramref name="capacity"/> of them.</summary>
    /// <param name="timeProvider">The clock of the idle times, the sweep and the training requests.</param>
    /// <param name="settings">Gives <see cref="ServerSetting.SessionExpireMinutes"/>.</param>
    /// <param name="capacity">How many accounts' books are kept at most.</param>
    public ClassModelCache(TimeProvider timeProvider, IOptions<ServerSetting> settings, int capacity = DefaultCapacity)
    {
        _timeProvider = timeProvider;
        _idleTimeout = TimeSpan.FromMinutes(settings.Value.SessionExpireMinutes);
        _capacity = capacity;
        _sweep = timeProvider.CreateTimer(_ => Sweep(), null, _sweepInterval, _sweepInterval);
    }

    /// <summary>How many accounts' books are kept (for the tests).</summary>
    internal int Count
    {
        get
        {
            lock (_lock) return _entries.Count;
        }
    }

    /// <summary>Stops the sweep.</summary>
    public void Dispose() => _sweep.Dispose();

    /// <summary>One account's book: its loaded model (with the file it came from) and the last training asked for.</summary>
    private sealed class Entry((string Book, string Email) key)
    {
        /// <summary>The account's book.</summary>
        public (string Book, string Email) Key { get; } = key;

        /// <summary>The file <see cref="Classifier"/> was read from.</summary>
        public string? FilePath { get; set; }

        /// <summary>The loaded model, or null when none was read yet.</summary>
        public ContentClassifier? Classifier { get; set; }

        /// <summary>The fingerprint of the records whose training was last asked for.</summary>
        public string? RequestedFingerprint { get; set; }

        /// <summary>When that training was asked for.</summary>
        public DateTimeOffset RequestedAt { get; set; }

        /// <summary>When the entry was last used.</summary>
        public DateTimeOffset LastUsed { get; set; }
    }

    /// <summary>Guards <see cref="_entries"/> and <see cref="_recency"/>.</summary>
    private readonly Lock _lock = new();

    /// <summary>The entries by account's book.</summary>
    private readonly Dictionary<(string Book, string Email), LinkedListNode<Entry>> _entries = [];

    /// <summary>The entries, most recently used first.</summary>
    private readonly LinkedList<Entry> _recency = [];

    /// <summary>Drops the entries unused for the session time; the least recently used are at the end of <see cref="_recency"/>.</summary>
    private void Sweep()
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            while (_recency.Last is { } last && now - last.Value.LastUsed >= _idleTimeout)
            {
                _recency.RemoveLast();
                _entries.Remove(last.Value.Key);
            }
        }
    }

    /// <summary>
    /// The entry of <paramref name="key"/>, created when missing, marked as used at <paramref name="now"/>; drops the least recently
    /// used past the capacity.
    /// </summary>
    private Entry Use((string Book, string Email) key, DateTimeOffset now)
    {
        if (_entries.TryGetValue(key, out LinkedListNode<Entry>? node))
        {
            _recency.Remove(node);
        }
        else
        {
            node = new LinkedListNode<Entry>(new Entry(key));
            _entries[key] = node;
        }

        _recency.AddFirst(node);
        node.Value.LastUsed = now;
        while (_recency.Count > _capacity && _recency.Last is { } last)
        {
            _recency.RemoveLast();
            _entries.Remove(last.Value.Key);
        }

        return node.Value;
    }

    /// <summary>The kept model of <paramref name="email"/>'s <paramref name="book"/> read from <paramref name="filePath"/>, or null.</summary>
    public ContentClassifier? Find(string book, string email, string filePath)
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (!_entries.TryGetValue((book, email), out LinkedListNode<Entry>? node) || !string.Equals(node.Value.FilePath, filePath, StringComparison.Ordinal))
                return null;
            return Use((book, email), now).Classifier;
        }
    }

    /// <summary>Keeps <paramref name="classifier"/>, read from <paramref name="filePath"/>, as <paramref name="email"/>'s <paramref name="book"/> model.</summary>
    public void Keep(string book, string email, string filePath, ContentClassifier classifier)
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Entry entry = Use((book, email), now);
            entry.FilePath = filePath;
            entry.Classifier = classifier;
        }
    }

    /// <summary>
    /// Whether a training of <paramref name="email"/>'s <paramref name="book"/> on the records of <paramref name="fingerprint"/> should be
    /// asked for now: true (and remembered) unless the same one was asked for within <see cref="TrainingRequestRetryAfter"/>.
    /// </summary>
    public bool ClaimTrainingRequest(string book, string email, string fingerprint)
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Entry entry = Use((book, email), now);
            if (string.Equals(entry.RequestedFingerprint, fingerprint, StringComparison.Ordinal) && now - entry.RequestedAt < TrainingRequestRetryAfter)
                return false;

            entry.RequestedFingerprint = fingerprint;
            entry.RequestedAt = now;
            return true;
        }
    }
}
