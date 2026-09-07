using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Board"/> domain objects.
/// Provides board-specific operations such as incrementing the view counter and returning the generated ID on write.
/// </summary>
public class BoardRepository(ApplicationDbContext context)
    : GenericRepository<Board, OrmBoard>(context), IBoardRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmBoard"/> rows.</summary>
    protected override DbSet<OrmBoard> Set => Context.Boards;

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id, CancellationToken ct = default)
        => await Context.Boards
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.View, b => b.View + 1), ct);

    /// <inheritdoc />
    public async Task<long> WriteBoardAsync(Board board, CancellationToken ct = default)
    {
        OrmBoard entity = ToEntity(board);
        _ = await Context.Boards.AddAsync(entity, ct);
        _ = await Context.SaveChangesAsync(ct);
        return entity.Id;
    }

    /// <summary>Maps a persisted <see cref="OrmBoard"/> row to the <see cref="Board"/> domain object.</summary>
    protected override Board ToDomain(OrmBoard e) => Board.Reconstitute(
        e.Id, e.Type ?? "", e.Title ?? "", e.Content,
        e.Writer ?? "", e.Created, e.Updated, e.View, e.Deleted, e.Locked, e.Noticed);

    /// <summary>Maps a <see cref="Board"/> domain object to its <see cref="OrmBoard"/> persistence representation.</summary>
    protected override OrmBoard ToEntity(Board b) => new()
    {
        Id = b.Id,
        Type = b.Type,
        Title = b.Title,
        Content = b.Content ?? "",
        Writer = b.Writer,
        Created = b.Created,
        Updated = b.Updated,
        View = b.View,
        Deleted = b.Deleted,
        Locked = b.Locked,
        Noticed = b.Noticed
    };

    /// <inheritdoc />
    public Task<List<Board>> GetByTypeAsync(string type, CancellationToken ct = default)
        => QueryAsync(e => e.Type == type && !e.Deleted, ct: ct);

    /// <inheritdoc />
    public Task<List<Board>> GetNoticedAsync(string type, CancellationToken ct = default)
        => QueryAsync(e => e.Type == type && !e.Deleted && e.Noticed,
            q => q.OrderByDescending(e => e.Id), ct);

    /// <inheritdoc />
    public async Task<(List<Board> Items, int TotalCount)> QueryPageAsync(BoardPageQuery query, CancellationToken ct = default)
    {
        IQueryable<OrmBoard> baseQuery = Set.Where(x => x.Type == query.Type && !x.Deleted && !x.Noticed);
        if (query.OwnerNickname != null)
            baseQuery = baseQuery.Where(x => x.Writer == query.OwnerNickname);

        IQueryable<OrmBoard> filtered;

        if (query.OwnerNickname != null)
        {
            // Already scoped to the caller's own posts (PrivateNote) — no role/Locked visibility
            // split needed, matching ManagementController's PrivateNote list action.
            IQueryable<OrmBoard> scoped = query.SearchType switch
            {
                "Title" when !string.IsNullOrEmpty(query.SearchText) => baseQuery.Where(x => x.Title.Contains(query.SearchText)),
                "Writer" when !string.IsNullOrEmpty(query.SearchText) => baseQuery.Where(x => x.Writer.Contains(query.SearchText)),
                _ => baseQuery
            };
            filtered = scoped.OrderByDescending(x => x.Id);
        }
        else if (query.SearchType == "Title" && !string.IsNullOrEmpty(query.SearchText))
        {
            IQueryable<OrmBoard> titleMatches = baseQuery.Where(x => x.Title.Contains(query.SearchText));

            if (query.IsLoggedIn)
            {
                if (query.ViewerRole == Role.Admin)
                {
                    // Administrator can search all posts, including locked ones.
                    filtered = titleMatches.OrderByDescending(x => x.Id);
                }
                else if (query.ViewerRole == Role.User)
                {
                    // User can search regular posts plus their own locked posts. Preserves the
                    // original's group order (non-locked matches first, then the searcher's own
                    // locked matches after — not a single Id ordering) via SQL UNION ALL (Concat),
                    // instead of materializing two lists in memory.
                    IQueryable<OrmBoard> noLocked = titleMatches.Where(x => !x.Locked);
                    IQueryable<OrmBoard> ownLocked = titleMatches.Where(x => x.Locked && x.Writer == query.ViewerNickname);

                    filtered = noLocked.Select(x => new { Board = x, Group = 0 })
                        .Concat(ownLocked.Select(x => new { Board = x, Group = 1 }))
                        .OrderBy(x => x.Group).ThenByDescending(x => x.Board.Id)
                        .Select(x => x.Board);
                }
                else
                {
                    // Logged in but neither Admin nor User (Role only ever has these two values for
                    // an active session — unreachable in practice). Matches the original code: no
                    // case fires, so the list is left as the unfiltered, unsearched base query.
                    filtered = baseQuery.OrderByDescending(x => x.Id);
                }
            }
            else
            {
                // Not logged in — private (locked) posts are never searchable.
                filtered = titleMatches.Where(x => !x.Locked).OrderByDescending(x => x.Id);
            }
        }
        else if (query.SearchType == "Writer" && !string.IsNullOrEmpty(query.SearchText))
        {
            filtered = baseQuery.Where(x => x.Writer.Contains(query.SearchText)).OrderByDescending(x => x.Id);
        }
        else
        {
            filtered = baseQuery.OrderByDescending(x => x.Id);
        }

        int totalCount = await filtered.CountAsync(ct);
        List<OrmBoard> page = await filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return (page.Select(ToDomain).ToList(), totalCount);
    }

    /// <inheritdoc />
    public Task<Board?> FindActiveByIdAsync(long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.Id == id && e.Deleted == false, ct);

    /// <inheritdoc />
    public async Task<Board?> FindActiveByIdForUpdateAsync(long id, CancellationToken ct = default)
    {
        // Raw SQL is required because EF Core LINQ cannot emit FOR UPDATE.
        // ToListAsync on the raw root executes the SQL verbatim (no composition),
        // keeping FOR UPDATE at the top level of the statement.
        // AsNoTracking is required too: if this Board's row was already tracked earlier in the
        // same DbContext scope, a tracking query performs identity resolution and hands back that
        // already-tracked (pre-lock) instance instead of the row this FOR UPDATE just (re-)read —
        // silently defeating the lock's whole purpose. AsNoTracking always materializes a fresh
        // instance from what was actually just selected, bypassing that snap-back.
        var entities = await Set.FromSqlInterpolated(
            $"""
             SELECT *
             FROM "Board"
             WHERE "Id" = {id} AND "Deleted" = false
             LIMIT 1
             FOR UPDATE
             """).AsNoTracking().ToListAsync(ct);

        var entity = entities.FirstOrDefault();
        return entity == null ? null : ToDomain(entity);
    }
}
