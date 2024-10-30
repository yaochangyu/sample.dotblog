namespace Lab.Sharding.WebAPI;

enum MyEnum
{
    AA = 0,
    BB
}

public class PaginatedList<T>
{
    public List<T> Items { get; }

    public int PageIndex { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => PageIndex > 1;

    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList()
    {
    }

    public PaginatedList(List<T> items, int pageIndex, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        Items = items;
        PageIndex = pageIndex;
        TotalPages = totalPages;
    }
}