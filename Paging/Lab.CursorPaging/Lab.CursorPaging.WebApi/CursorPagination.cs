namespace Lab.CursorPaging.WebApi;

public class CursorPagination<T>
{
    public List<T> Items { get; set; }

    public string NextCursorToken { get; set; }

    public string PreviousCursorToken { get; set; }

    public bool HasNext => string.IsNullOrWhiteSpace(this.NextCursorToken) == false;

    public bool HasPrevious => string.IsNullOrWhiteSpace(this.PreviousCursorToken) == false;
}