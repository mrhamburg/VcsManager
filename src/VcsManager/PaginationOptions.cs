namespace VcsManager;

/// <summary>
/// Describes a page to request from a provider.
/// </summary>
public record PaginationOptions
{
    /// <summary>
    /// The default page number.
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// The default page size.
    /// </summary>
    public const int DefaultPageSize = 30;

    /// <summary>
    /// The maximum page size supported by all providers.
    /// </summary>
    public const int MaxPageSize = 100;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Gets the page number, starting at 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int Page
    {
        get => _page;
        init
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Page),
                    value,
                    "Page must be greater than or equal to 1.");
            }

            _page = value;
        }
    }

    /// <summary>
    /// Gets the page size, between 1 and <see cref="MaxPageSize"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 1 or greater than <see cref="MaxPageSize"/>.
    /// </exception>
    public int PageSize
    {
        get => _pageSize;
        init
        {
            if (value < 1 || value > MaxPageSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PageSize),
                    value,
                    $"PageSize must be between 1 and {MaxPageSize}.");
            }

            _pageSize = value;
        }
    }
}
