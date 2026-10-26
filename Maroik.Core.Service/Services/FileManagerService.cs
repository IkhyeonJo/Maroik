using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.FileManagers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IFileManagerService"/>: a user's drive of logical folders (database only) and files stored under GUID
/// names in file storage. The rules live in the domain (<see cref="FileManagerNode"/>, <see cref="FileManagerTree"/>,
/// <see cref="FileManagerNamePolicy"/>, <see cref="FileManagerUploadPolicy"/>, <see cref="FileManagerPreviewPolicy"/>); this service
/// orchestrates them.
/// <para>
/// Every structural write runs in its own transaction and first locks the account row (<c>FindByEmailForUpdateAsync</c>), so one
/// user's concurrent uploads, moves and deletes are applied one at a time: two uploads can never both pass the quota check (Q4), and a
/// name checked free stays free until the commit. Work on file storage — a network call — never runs inside a transaction: a delete
/// commits the rows first and then asks file storage to delete the files, leaving an orphan file (logged) rather than a row that points
/// at a missing file (P4).
/// </para>
/// </summary>
public class FileManagerService(
    IFileManagerNodeRepository nodeRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IFileClient fileClient,
    IOptions<ServerSetting> settings,
    ILogger<FileManagerService> logger,
    TimeProvider timeProvider) : IFileManagerService
{
    /// <summary>The base URL of the file-storage service.</summary>
    private string BaseUrl => settings.Value.FileStorageBaseUrl ?? "";

    /// <summary>The current UTC time.</summary>
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>The answer to an item the account does not have.</summary>
    private static ServiceResult ItemNotFound() => ServiceResult.NotFound("FileManager.NotFound", ServiceErrorKeys.FileManagerItemNotFound);

    /// <summary>The answer to an upload the account does not have.</summary>
    private static ServiceResult UploadNotFound() => ServiceResult.NotFound("FileManager.UploadNotFound", ServiceErrorKeys.FileManagerUploadNotFound);

    /// <summary>The answer to a name already used in the folder.</summary>
    private static ServiceResult NameTaken(string name) => ServiceResult.Conflict("FileManager.NameTaken", ServiceErrorKeys.FileManagerNameTaken, name);

    /// <summary>The answer to an unexpected (infrastructure) failure.</summary>
    private static ServiceResult Temporary() => ServiceResult.Failure("FileManager.Failed", ServiceResult.TemporaryErrorKey);

    /// <summary>The account's whole drive.</summary>
    private async Task<FileManagerTree> LoadTreeAsync(string accountEmail, CancellationToken ct) =>
        new(await nodeRepository.GetByAccountEmailAsync(accountEmail, ct));

    /// <summary>Whether <paramref name="folderId"/> is the root or a folder of the drive.</summary>
    private static bool IsFolderOf(FileManagerTree tree, long? folderId) => folderId is not { } id || tree.Find(id) is { IsFolder: true };

    /// <summary>The finished node with this id, or null (another account's, missing, or an unfinished upload).</summary>
    private static FileManagerNode? Finished(FileManagerTree tree, long id) => tree.Find(id) is { Uploading: false } node ? node : null;

    /// <summary>The finished items directly in <paramref name="folderId"/>.</summary>
    private static IEnumerable<FileManagerNode> FinishedChildren(FileManagerTree tree, long? folderId) => tree.Children(folderId).Where(n => !n.Uploading);

    /// <summary>The bytes the drive uses: every file, unfinished uploads included (Q4).</summary>
    private static long UsedBytes(IEnumerable<FileManagerNode> nodes) => nodes.Where(n => !n.IsFolder).Sum(n => n.Size);

    /// <summary>Folders first, then by name ignoring case (then exactly, so the order is stable).</summary>
    private static IEnumerable<FileManagerNode> Sorted(IEnumerable<FileManagerNode> nodes) =>
        nodes.OrderBy(n => !n.IsFolder).ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ThenBy(n => n.Name, StringComparer.Ordinal);

    /// <summary>The list entry of <paramref name="node"/>; with its location when <paramref name="withLocation"/>.</summary>
    private static FileManagerItemResponse ToItem(FileManagerTree tree, FileManagerNode node, bool withLocation = false) => new()
    {
        Id = node.Id,
        ParentId = node.ParentId,
        Name = node.Name,
        IsFolder = node.IsFolder,
        Size = node.Size,
        Updated = node.Updated,
        ItemCount = node.IsFolder ? tree.ItemCount(node.Id) : 0,
        Previewable = node.IsPreviewable,
        Location = withLocation ? [.. tree.Location(node.Id)] : []
    };

    /// <summary>The storage path of the upload <paramref name="uploadId"/>.</summary>
    private static string StoragePathOf(Guid uploadId) => $"{FileManagerUploadPolicy.StorageDirectory}/{uploadId:D}";

    /// <summary>The account's unfinished upload <paramref name="uploadId"/>, or null.</summary>
    private async Task<FileManagerNode?> FindUploadAsync(string accountEmail, Guid uploadId, CancellationToken ct) =>
        await nodeRepository.FindByEmailAndStoragePathAsync(accountEmail, StoragePathOf(uploadId), ct) is { Uploading: true } node ? node : null;

    /// <summary>Asks file storage to delete a stored file; one it cannot delete is left as an orphan and logged.</summary>
    private async Task DeleteStoredFileAsync(string accountEmail, FileManagerNode file, CancellationToken ct)
    {
        if (file.Uploading)
        {
            if (file.StorageId is { } uploadId && !await fileClient.AbortFileManagerUploadAsync(uploadId, BaseUrl, ct))
                logger.LogWarning("File Manager: unfinished upload {UploadId} of {Email} could not be aborted", uploadId, accountEmail);
            return;
        }

        if (file.StoragePath is { } path && !await fileClient.DeleteFileManagerFileAsync(path, BaseUrl, ct))
            logger.LogWarning("File Manager: stored file {FilePath} of {Email} could not be deleted; left as an orphan", path, accountEmail);
    }

    // -- Reads --------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FileManagerDriveResponse> GetDriveAsync(string accountEmail, CancellationToken ct = default)
    {
        FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
        long quota = (await accountRepository.FindByEmailAsync(accountEmail, ct))?.FileManagerQuotaBytes ?? 0;
        return new FileManagerDriveResponse
        {
            UsedBytes = UsedBytes(tree.Nodes),
            QuotaBytes = quota,
            RootItemCount = tree.ItemCount(null),
            Folders = [.. Sorted(tree.Nodes.Where(n => n.IsFolder)).Select(n => ToItem(tree, n))]
        };
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, FileManagerFolderResponse? Folder)> GetFolderAsync(string accountEmail, long? folderId, CancellationToken ct = default)
    {
        FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
        if (!IsFolderOf(tree, folderId))
            return (ItemNotFound(), null);

        return (ServiceResult.Ok(), new FileManagerFolderResponse
        {
            FolderId = folderId,
            Breadcrumb = [.. tree.Ancestry(folderId).Reverse().Select(id => new FileManagerCrumbResponse { Id = id, Name = tree.Find(id)!.Name })],
            Items = [.. Sorted(FinishedChildren(tree, folderId)).Select(n => ToItem(tree, n))]
        });
    }

    /// <inheritdoc />
    public async Task<List<FileManagerItemResponse>> SearchAsync(string accountEmail, string? query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        string term = query.Trim();
        FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
        return [.. Sorted(tree.Nodes.Where(n => !n.Uploading && n.Name.Contains(term, StringComparison.OrdinalIgnoreCase))).Select(n => ToItem(tree, n, withLocation: true))];
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, List<FileManagerItemResponse> Files)> ListFilesForDownloadAsync(string accountEmail, IReadOnlyCollection<long> ids, CancellationToken ct = default)
    {
        FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
        Dictionary<long, FileManagerNode> files = [];
        foreach (long id in ids)
        {
            if (Finished(tree, id) is not { } node)
                return (ItemNotFound(), []);

            IEnumerable<FileManagerNode> found = node.IsFolder ? tree.Descendants(node.Id).Where(n => !n.IsFolder && !n.Uploading) : [node];
            foreach (FileManagerNode file in found)
                files.TryAdd(file.Id, file);
        }

        return (ServiceResult.Ok(), [.. files.Values.Select(n => ToItem(tree, n))]);
    }

    // -- Structural writes ----------------------------------------------------------------

    /// <inheritdoc />
    public async Task<(ServiceResult Result, long? FolderId)> CreateFolderAsync(string accountEmail, long? parentId, string? name, bool merge, CancellationToken ct = default)
    {
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            if (!IsFolderOf(tree, parentId))
            {
                await unitOfWork.RollbackAsync(ct);
                return (ItemNotFound(), null);
            }

            var created = FileManagerNode.CreateFolder(accountEmail, parentId, name, UtcNow);
            if (created.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return (ServiceResult.FromError(created.FirstError), null);
            }

            FileManagerNode folder = created.Value;
            if (FinishedChildren(tree, parentId).FirstOrDefault(n => n.Name == folder.Name) is { } existing)
            {
                await unitOfWork.RollbackAsync(ct);
                // A folder upload merges into a folder of the same name (F2-C); anything else is a taken name.
                return merge && existing.IsFolder ? (ServiceResult.Ok(), existing.Id) : (NameTaken(folder.Name), null);
            }

            await nodeRepository.CreateAsync(folder, ct);
            await unitOfWork.CommitAsync(ct);
            long id = (await nodeRepository.GetByAccountEmailAsync(accountEmail, ct))
                .Single(n => n.IsFolder && n.ParentId == parentId && n.Name == folder.Name).Id;
            return (ServiceResult.Ok(), id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to create a folder for {Email}", accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return (Temporary(), null);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> RenameAsync(string accountEmail, long id, string? name, CancellationToken ct = default)
    {
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            if (Finished(tree, id) is not { } node)
            {
                await unitOfWork.RollbackAsync(ct);
                return ItemNotFound();
            }

            var renamed = node.Rename(name, UtcNow);
            if (renamed.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return ServiceResult.FromError(renamed.FirstError);
            }

            if (FinishedChildren(tree, node.ParentId).Any(n => n.Id != id && n.Name == node.Name))
            {
                await unitOfWork.RollbackAsync(ct);
                return NameTaken(node.Name);
            }

            await nodeRepository.UpdateEntityAsync(node, ct);
            await unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to rename item {ItemId} of {Email}", id, accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return Temporary();
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> MoveAsync(string accountEmail, IReadOnlyCollection<long> ids, long? targetFolderId, CancellationToken ct = default)
    {
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            List<FileManagerNode?> found = [.. ids.Distinct().Select(id => Finished(tree, id))];
            if (!IsFolderOf(tree, targetFolderId) || found.Any(n => n == null))
            {
                await unitOfWork.RollbackAsync(ct);
                return ItemNotFound();
            }

            List<FileManagerNode> moved = [.. found.OfType<FileManagerNode>()];
            IReadOnlyList<long> ancestry = tree.Ancestry(targetFolderId);
            DateTime utcNow = UtcNow;
            foreach (FileManagerNode node in moved)
            {
                var result = node.MoveTo(targetFolderId, ancestry, utcNow);
                if (result.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return ServiceResult.FromError(result.FirstError);
                }
            }

            // The names the target folder would hold: what stays there plus everything moved in.
            HashSet<long> movedIds = [.. moved.Select(n => n.Id)];
            List<string> names = [.. FinishedChildren(tree, targetFolderId).Where(n => !movedIds.Contains(n.Id)).Select(n => n.Name), .. moved.Select(n => n.Name)];
            if (names.GroupBy(n => n, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
            {
                await unitOfWork.RollbackAsync(ct);
                return NameTaken(duplicate.Key);
            }

            foreach (FileManagerNode node in moved)
                await nodeRepository.UpdateEntityAsync(node, ct);
            await unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to move {Count} item(s) of {Email}", ids.Count, accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return Temporary();
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, IReadOnlyCollection<long> ids, CancellationToken ct = default)
    {
        List<FileManagerNode> removed;
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            List<FileManagerNode?> selected = [.. ids.Distinct().Select(tree.Find)];
            if (selected.Any(n => n == null))
            {
                await unitOfWork.RollbackAsync(ct);
                return ItemNotFound();
            }

            removed = [.. selected.OfType<FileManagerNode>().SelectMany(n => n.IsFolder ? [n, .. tree.Descendants(n.Id)] : new[] { n }).DistinctBy(n => n.Id)];
            await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [.. removed.Select(n => n.Id)], ct);
            await unitOfWork.CommitAsync(ct);
            logger.LogInformation("File Manager: {Email} deleted {Count} item(s) ({Nodes} node(s), {Files} stored file(s))",
                accountEmail, selected.Count, removed.Count, removed.Count(n => !n.IsFolder));
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to delete {Count} item(s) of {Email}", ids.Count, accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return Temporary();
        }

        // After the commit: a row never points at a missing file; a file that cannot be deleted is left as an orphan (P4).
        foreach (FileManagerNode file in removed.Where(n => !n.IsFolder))
            await DeleteStoredFileAsync(accountEmail, file, ct);
        return ServiceResult.Ok();
    }

    // -- Uploads -----------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<ServiceResult> CheckQuotaAsync(string accountEmail, long totalBytes, CancellationToken ct = default)
    {
        long quota = (await accountRepository.FindByEmailAsync(accountEmail, ct))?.FileManagerQuotaBytes ?? 0;
        FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
        // Abandoned uploads do not count, as in BeginUploadAsync (Q5): an account they filled must still reach the upload that removes them.
        DateTime utcNow = UtcNow;
        var result = FileManagerUploadPolicy.CheckQuota(quota, UsedBytes(tree.Nodes.Where(n => !n.IsStaleUpload(utcNow))), totalBytes);
        return result.IsError ? ServiceResult.FromError(result.FirstError) : ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, FileManagerUploadStart? Start)> BeginUploadAsync(string accountEmail, FileManagerUploadRequest request, CancellationToken ct = default)
    {
        List<FileManagerNode> stale;
        FileManagerUploadStart start;
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            Account? account = await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            long quota = account?.FileManagerQuotaBytes ?? 0;
            // A file too large to be scanned can only be stored without a scan: it is started only for an account whose scan is off.
            bool scanDisabled = account?.FileManagerScanDisabled == true;
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            DateTime utcNow = UtcNow;
            if (!IsFolderOf(tree, request.ParentId))
            {
                await unitOfWork.RollbackAsync(ct);
                return (ItemNotFound(), null);
            }

            Guid storageId = Guid.NewGuid();
            var started = FileManagerNode.StartUpload(accountEmail, request.ParentId, request.Name, request.Size, storageId, utcNow, scanDisabled);
            if (started.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return (ServiceResult.FromError(started.FirstError), null);
            }

            // Uploads idle for more than an hour are removed now (Q5), ahead of the worker's hourly sweep: they stop counting against the quota.
            stale = [.. tree.Nodes.Where(n => n.IsStaleUpload(utcNow))];
            string name = started.Value.Name;
            if (FinishedChildren(tree, request.ParentId).FirstOrDefault(n => n.Name == name) is { } existing)
            {
                switch (request.Conflict)
                {
                    case FileManagerConflictChoice.Ask:
                        await unitOfWork.RollbackAsync(ct);
                        return (ServiceResult.Ok(), new FileManagerUploadStart(true, Guid.Empty, name, FileManagerUploadPolicy.ChunkBytes));
                    case FileManagerConflictChoice.KeepBoth:
                        name = FileManagerNamePolicy.KeepBothName(name, [.. tree.Children(request.ParentId).Except(stale).Select(n => n.Name)]);
                        break;
                    // A file never overwrites a folder.
                    case FileManagerConflictChoice.Overwrite when existing.IsFolder:
                        await unitOfWork.RollbackAsync(ct);
                        return (NameTaken(name), null);
                }
            }

            long used = UsedBytes(tree.Nodes.Except(stale));
            var quotaResult = FileManagerUploadPolicy.CheckQuota(quota, used, request.Size);
            if (quotaResult.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                logger.LogWarning("File Manager upload refused for {Email}: over quota ({Requested} bytes requested, {Used} used of {Quota})",
                    accountEmail, request.Size, used, quota);
                return (ServiceResult.FromError(quotaResult.FirstError), null);
            }

            if (stale.Count > 0)
                await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [.. stale.Select(n => n.Id)], ct);
            FileManagerNode upload = FileManagerNode.StartUpload(accountEmail, request.ParentId, name, request.Size, storageId, utcNow, scanDisabled).Value;
            await nodeRepository.CreateAsync(upload, ct);
            await unitOfWork.CommitAsync(ct);
            start = new FileManagerUploadStart(false, storageId, name, FileManagerUploadPolicy.ChunkBytes);
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to start an upload for {Email}", accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return (Temporary(), null);
        }

        if (stale.Count > 0)
        {
            foreach (FileManagerNode upload in stale)
                await DeleteStoredFileAsync(accountEmail, upload, ct);
            logger.LogInformation("File Manager: removed {Count} abandoned upload(s) of {Email}", stale.Count, accountEmail);
        }

        return (ServiceResult.Ok(), start);
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, long Length)> AppendChunkAsync(string accountEmail, Guid uploadId, long offset, Stream chunk, long chunkLength, CancellationToken ct = default)
    {
        if (await FindUploadAsync(accountEmail, uploadId, ct) is not { } upload)
            return (UploadNotFound(), 0);

        // Nothing beyond the declared (and reserved) size is accepted.
        if (offset < 0 || offset + chunkLength > upload.Size)
            return (ServiceResult.Validation("FileManager.ChunkInvalid", ServiceErrorKeys.InputInvalid), 0);

        long? length = await fileClient.AppendFileManagerChunkAsync(uploadId, offset, chunk, BaseUrl, ct);
        if (length is { } received)
        {
            // A stored chunk keeps the upload alive (Q5), however long the whole upload takes.
            await nodeRepository.RecordUploadActivityAsync(accountEmail, upload.Id, UtcNow, ct);
            return (ServiceResult.Ok(), received);
        }

        logger.LogWarning("File Manager: chunk at {Offset} of upload {UploadId} of {Email} was not stored", offset, uploadId, accountEmail);
        return (Temporary(), 0);
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, FileManagerCompletion Completion)> CompleteUploadAsync(string accountEmail, Guid uploadId, bool overwrite, CancellationToken ct = default)
    {
        if (await nodeRepository.FindByEmailAndStoragePathAsync(accountEmail, StoragePathOf(uploadId), ct) is not { } upload)
            return (UploadNotFound(), FileManagerCompletion.Decided);
        // The page asks again while the scan runs, so an upload completed already (a repeated or a second answer) answers the same.
        if (!upload.Uploading)
            return (ServiceResult.Ok(), FileManagerCompletion.Decided);

        // Read when the upload completes, so an administrator's change made while it was sent is the one that counts.
        bool scanDisabled = (await accountRepository.FindByEmailAsync(accountEmail, ct))?.FileManagerScanDisabled == true;
        FileUploadResult outcome = await fileClient.CompleteFileManagerUploadAsync(uploadId, upload.Size, scanDisabled, BaseUrl, ct);
        // Still scanning in the background: nothing is decided, the reservation stays, and the page's poll keeps the upload alive (Q5).
        if (outcome == FileUploadResult.Scanning)
        {
            await nodeRepository.RecordUploadActivityAsync(accountEmail, upload.Id, UtcNow, ct);
            return (ServiceResult.Ok(), FileManagerCompletion.Scanning);
        }

        if (outcome != FileUploadResult.Stored)
        {
            // The file is not kept (file storage already dropped it): release the reservation.
            await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [upload.Id], ct);
            switch (outcome)
            {
                case FileUploadResult.Infected:
                    logger.LogWarning("File Manager upload refused for {Email}: file {ItemId} infected", accountEmail, upload.Id);
                    return (ServiceResult.Validation("FileManager.Infected", ServiceErrorKeys.FileManagerInfected), FileManagerCompletion.Decided);
                case FileUploadResult.ScanUnavailable:
                case FileUploadResult.ScanLimitExceeded:
                    logger.LogWarning("File Manager upload refused for {Email}: file {ItemId} could not be scanned", accountEmail, upload.Id);
                    return (ServiceResult.Failure("FileManager.ScanUnavailable", ServiceErrorKeys.FileManagerScanUnavailable), FileManagerCompletion.Decided);
                default:
                    logger.LogWarning("File Manager upload of file {ItemId} of {Email} could not be stored", upload.Id, accountEmail);
                    return (Temporary(), FileManagerCompletion.Decided);
            }
        }

        return (await FinishUploadAsync(accountEmail, uploadId, upload, overwrite, scanDisabled, ct), FileManagerCompletion.Decided);
    }

    /// <summary>
    /// Makes an upload whose file file storage has stored a normal item of the drive, under the account lock: completed meanwhile is a
    /// success, removed meanwhile is "not found", a name taken meanwhile (without <paramref name="overwrite"/>) fails, and an overwrite
    /// replaces the old file. A file stored with the account's virus scan off (<paramref name="scanDisabled"/>) is an audit event. The
    /// file nobody keeps, or the one an overwrite replaced, is deleted after the commit (P4).
    /// </summary>
    private async Task<ServiceResult> FinishUploadAsync(string accountEmail, Guid uploadId, FileManagerNode upload, bool overwrite, bool scanDisabled, CancellationToken ct)
    {
        FileManagerNode? replaced = null;
        FileManagerNode? discarded = null;
        ServiceResult result;
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            FileManagerTree tree = await LoadTreeAsync(accountEmail, ct);
            FileManagerNode? current = tree.Find(upload.Id);
            FileManagerNode? sibling = current == null ? null : FinishedChildren(tree, current.ParentId).FirstOrDefault(n => n.Name == current.Name);
            if (current is { Uploading: false })
            {
                // Completed meanwhile by another answer for the same upload: the file is kept, nothing to write.
                await unitOfWork.RollbackAsync(ct);
                result = ServiceResult.Ok();
            }
            else if (current == null)
            {
                // Removed meanwhile (its folder was deleted): the stored file has no row to belong to.
                await unitOfWork.RollbackAsync(ct);
                discarded = upload;
                result = UploadNotFound();
            }
            else if (sibling != null && (!overwrite || sibling.IsFolder))
            {
                await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [current.Id], ct);
                await unitOfWork.CommitAsync(ct);
                discarded = upload;
                result = NameTaken(current.Name);
            }
            else
            {
                if (sibling != null)
                {
                    await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [sibling.Id], ct);
                    replaced = sibling;
                }

                current.CompleteUpload(UtcNow);
                await nodeRepository.UpdateEntityAsync(current, ct);
                await unitOfWork.CommitAsync(ct);
                if (scanDisabled)
                    logger.LogWarning("File Manager: {Email} stored file {ItemId} ({Size} bytes) with the virus scan turned off", accountEmail, current.Id, current.Size);
                result = ServiceResult.Ok();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to complete upload {UploadId} of {Email}", uploadId, accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return Temporary();
        }

        // After the commit (P4): drop the file nobody keeps, or the one an overwrite replaced.
        if (discarded != null && !await fileClient.DeleteFileManagerFileAsync(StoragePathOf(uploadId), BaseUrl, ct))
            logger.LogWarning("File Manager: stored file {FilePath} of {Email} could not be deleted; left as an orphan", StoragePathOf(uploadId), accountEmail);
        if (replaced != null)
        {
            logger.LogInformation("File Manager: {Email} overwrote file {ItemId}", accountEmail, replaced.Id);
            await DeleteStoredFileAsync(accountEmail, replaced, ct);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ServiceResult> AbortUploadAsync(string accountEmail, Guid uploadId, CancellationToken ct = default)
    {
        if (await FindUploadAsync(accountEmail, uploadId, ct) is not { } upload)
            return UploadNotFound();

        await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [upload.Id], ct);
        await DeleteStoredFileAsync(accountEmail, upload, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<int> RemoveStaleUploadsAsync(int maxAccounts, CancellationToken ct = default)
    {
        DateTime utcNow = UtcNow;
        int removed = 0;
        foreach (string accountEmail in await nodeRepository.ListAccountsWithUploadsIdleBeforeAsync(utcNow - FileManagerUploadPolicy.StaleUploadAge, maxAccounts, ct))
            removed += await RemoveStaleUploadsOfAsync(accountEmail, utcNow, ct);
        return removed;
    }

    /// <summary>
    /// Removes the account's idle uploads under its account lock — judged again there, since a chunk may have arrived after the unlocked
    /// listing — and then aborts them in file storage (P4). A failure is logged and rolled back; the next sweep tries again.
    /// </summary>
    private async Task<int> RemoveStaleUploadsOfAsync(string accountEmail, DateTime utcNow, CancellationToken ct)
    {
        List<FileManagerNode> stale;
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            await accountRepository.FindByEmailForUpdateAsync(accountEmail, ct);
            stale = [.. (await LoadTreeAsync(accountEmail, ct)).Nodes.Where(n => n.IsStaleUpload(utcNow))];
            if (stale.Count == 0)
            {
                await unitOfWork.RollbackAsync(ct);
                return 0;
            }

            await nodeRepository.DeleteByEmailAndIdsAsync(accountEmail, [.. stale.Select(n => n.Id)], ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: failed to remove the abandoned uploads of {Email}", accountEmail);
            await unitOfWork.RollbackAsync(ct);
            return 0;
        }

        foreach (FileManagerNode upload in stale)
            await DeleteStoredFileAsync(accountEmail, upload, ct);
        logger.LogInformation("File Manager: removed {Count} abandoned upload(s) of {Email}", stale.Count, accountEmail);
        return stale.Count;
    }

    // -- Download / preview ------------------------------------------------------------------

    /// <summary>The account's finished file with this id, or null.</summary>
    private async Task<FileManagerNode?> FindFileAsync(string accountEmail, long id, CancellationToken ct) =>
        await nodeRepository.FindByEmailAndIdAsync(accountEmail, id, ct) is { IsFolder: false, Uploading: false } file ? file : null;

    /// <inheritdoc />
    public async Task<(ServiceResult Result, AttachmentDownload? File)> OpenFileAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        if (await FindFileAsync(accountEmail, id, ct) is not { StoragePath: { } path } file)
            return (ItemNotFound(), null);

        try
        {
            Stream content = await fileClient.OpenReadAsync(path, BaseUrl, ct);
            return (ServiceResult.Ok(), new AttachmentDownload(content, file.Name));
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: stored file {FilePath} could not be opened", path);
            return (Temporary(), null);
        }
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, FileManagerPreview? Preview)> OpenPreviewAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        if (await FindFileAsync(accountEmail, id, ct) is not { StoragePath: { } path } file)
            return (ItemNotFound(), null);

        ServiceResult noPreview = ServiceResult.Validation("FileManager.NoPreview", ServiceErrorKeys.FileManagerNoPreview);
        if (!file.IsPreviewable)
            return (noPreview, null);

        byte[] bytes;
        try
        {
            bytes = await fileClient.DownloadAsync(path, BaseUrl, ct);
        }
        catch (Exception e)
        {
            logger.LogError(e, "File Manager: stored file {FilePath} could not be opened", path);
            return (Temporary(), null);
        }

        // The extension is not trusted: only an image the leading bytes prove is previewed, under the Content-Type they prove.
        return FileManagerPreviewPolicy.ContentTypeOf(bytes) is { } contentType
            ? (ServiceResult.Ok(), new FileManagerPreview(bytes, contentType))
            : (noPreview, null);
    }
}
