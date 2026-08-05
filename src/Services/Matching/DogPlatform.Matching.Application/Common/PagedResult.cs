namespace DogPlatform.Matching.Application.Common;

public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public static PagedResult<T> Create(
        IReadOnlyCollection<T> items, int totalItems, int pageNumber, int pageSize) =>
        new()
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
}
