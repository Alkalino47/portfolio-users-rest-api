namespace portfolio_users_rest_api.Application.Common;

public class PaginatedResult<T>
{
    public required PaginationMetadata Pagination { get; init; }

    public required IReadOnlyList<T> Items { get; init; }
}

public class PaginationMetadata
{
    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
