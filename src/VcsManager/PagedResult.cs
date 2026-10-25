namespace VcsManager;

/// <summary>
/// Represents one page of results from a provider.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public record PagedResult<T>
{
    /// <summary>
    /// Gets the items on this page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets the page number.
    /// </summary>
    public int Page { get; init; } = PaginationOptions.DefaultPage;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; } = PaginationOptions.DefaultPageSize;

    /// <summary>
    /// Gets the total item count, or <see langword="null"/> when the provider does not report one.
    /// </summary>
    public int? TotalCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether another page is available.
    /// </summary>
    public bool HasNextPage { get; init; }
}
