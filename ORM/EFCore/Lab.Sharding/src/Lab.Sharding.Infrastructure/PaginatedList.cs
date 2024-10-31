namespace Lab.Sharding.Infrastructure;

enum MyEnum
{
	AA = 0,
	BB
}

public class PaginatedList<T>
{
	public List<T> Items { get; set; }

	public int PageIndex { get; set; }

	public int TotalPages { get; set; }

	public bool HasPreviousPage => this.PageIndex > 1;

	public bool HasNextPage => this.PageIndex < this.TotalPages;

	public PaginatedList()
	{
	}

	public PaginatedList(List<T> items, int pageIndex, int pageSize, int totalCount)
	{
		var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

		this.Items = items;
		this.PageIndex = pageIndex;
		this.TotalPages = totalPages;
	}
}