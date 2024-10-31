namespace Lab.Sharding.WebAPI;

public class CursorPaginatedList<T>
{
	public List<T> Items { get; }

	public string NextPageToken { get; set; }

	public string NextPreviousToken { get; set; }

	public CursorPaginatedList(List<T> items, string nextPageToken, string nextPreviousToken)
	{
		this.Items = items;
		this.NextPageToken = nextPageToken;
		this.NextPreviousToken = nextPreviousToken;
	}
}