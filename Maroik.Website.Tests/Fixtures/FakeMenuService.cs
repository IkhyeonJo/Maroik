using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IMenuService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeMenuService : IMenuService
{
    /// <summary>What <c>GetAllCategoriesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<IEnumerable<CategoryResponse>>>? OnGetAllCategoriesAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllCategoriesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllCategoriesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync(CancellationToken ct)
    {
        lock (GetAllCategoriesAsyncCalls) GetAllCategoriesAsyncCalls.Add(ct);
        if (OnGetAllCategoriesAsync is { } on)
        {
            return on(ct);
        }

        return Task.FromResult<IEnumerable<CategoryResponse>>([]);
    }

    /// <summary>What <c>GetAllSubCategoriesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<IEnumerable<SubCategoryResponse>>>? OnGetAllSubCategoriesAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllSubCategoriesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllSubCategoriesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync(CancellationToken ct)
    {
        lock (GetAllSubCategoriesAsyncCalls) GetAllSubCategoriesAsyncCalls.Add(ct);
        if (OnGetAllSubCategoriesAsync is { } on)
        {
            return on(ct);
        }

        return Task.FromResult<IEnumerable<SubCategoryResponse>>([]);
    }

    /// <summary>What <c>SearchCategoriesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, CancellationToken, Task<IEnumerable<CategoryResponse>>>? OnSearchCategoriesAsync { get; set; }

    /// <summary>The arguments of every <c>SearchCategoriesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string? Search, CancellationToken Ct)> SearchCategoriesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IEnumerable<CategoryResponse>> SearchCategoriesAsync(string? search, CancellationToken ct)
    {
        lock (SearchCategoriesAsyncCalls) SearchCategoriesAsyncCalls.Add((search, ct));
        if (OnSearchCategoriesAsync is { } on)
        {
            return on(search, ct);
        }

        return Task.FromResult<IEnumerable<CategoryResponse>>([]);
    }

    /// <summary>What <c>SearchSubCategoriesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, CancellationToken, Task<IEnumerable<SubCategoryResponse>>>? OnSearchSubCategoriesAsync { get; set; }

    /// <summary>The arguments of every <c>SearchSubCategoriesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string? Search, CancellationToken Ct)> SearchSubCategoriesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IEnumerable<SubCategoryResponse>> SearchSubCategoriesAsync(string? search, CancellationToken ct)
    {
        lock (SearchSubCategoriesAsyncCalls) SearchSubCategoriesAsyncCalls.Add((search, ct));
        if (OnSearchSubCategoriesAsync is { } on)
        {
            return on(search, ct);
        }

        return Task.FromResult<IEnumerable<SubCategoryResponse>>([]);
    }

    /// <summary>What <c>GetCategoryByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<int, CancellationToken, Task<CategoryResponse?>>? OnGetCategoryByIdAsync { get; set; }

    /// <summary>The arguments of every <c>GetCategoryByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(int Id, CancellationToken Ct)> GetCategoryByIdAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken ct)
    {
        lock (GetCategoryByIdAsyncCalls) GetCategoryByIdAsyncCalls.Add((id, ct));
        if (OnGetCategoryByIdAsync is { } on)
        {
            return on(id, ct);
        }

        return Task.FromResult<CategoryResponse?>(default);
    }

    /// <summary>What <c>GetSubCategoryByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<int, CancellationToken, Task<SubCategoryResponse?>>? OnGetSubCategoryByIdAsync { get; set; }

    /// <summary>The arguments of every <c>GetSubCategoryByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(int Id, CancellationToken Ct)> GetSubCategoryByIdAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id, CancellationToken ct)
    {
        lock (GetSubCategoryByIdAsyncCalls) GetSubCategoryByIdAsyncCalls.Add((id, ct));
        if (OnGetSubCategoryByIdAsync is { } on)
        {
            return on(id, ct);
        }

        return Task.FromResult<SubCategoryResponse?>(default);
    }

    /// <summary>What <c>CreateCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnCreateCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>CreateCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CategoryRequest Request, string ActorEmail, CancellationToken Ct)> CreateCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CreateCategoryAsync(CategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (CreateCategoryAsyncCalls) CreateCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnCreateCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnUpdateCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CategoryRequest Request, string ActorEmail, CancellationToken Ct)> UpdateCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (UpdateCategoryAsyncCalls) UpdateCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnUpdateCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnDeleteCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CategoryRequest Request, string ActorEmail, CancellationToken Ct)> DeleteCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (DeleteCategoryAsyncCalls) DeleteCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnDeleteCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>CreateSubCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<SubCategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnCreateSubCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>CreateSubCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(SubCategoryRequest Request, string ActorEmail, CancellationToken Ct)> CreateSubCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (CreateSubCategoryAsyncCalls) CreateSubCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnCreateSubCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateSubCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<SubCategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnUpdateSubCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateSubCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(SubCategoryRequest Request, string ActorEmail, CancellationToken Ct)> UpdateSubCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (UpdateSubCategoryAsyncCalls) UpdateSubCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnUpdateSubCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteSubCategoryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<SubCategoryRequest, string, CancellationToken, Task<ServiceResult>>? OnDeleteSubCategoryAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteSubCategoryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(SubCategoryRequest Request, string ActorEmail, CancellationToken Ct)> DeleteSubCategoryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request, string actorEmail, CancellationToken ct)
    {
        lock (DeleteSubCategoryAsyncCalls) DeleteSubCategoryAsyncCalls.Add((request, actorEmail, ct));
        if (OnDeleteSubCategoryAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }
}
