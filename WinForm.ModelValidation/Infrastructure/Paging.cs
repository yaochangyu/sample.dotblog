namespace Infrastructure
{
    public class Paging
    {
        public int TotalCount { get; set; }

        public int Skip { get; set; }

        public int RowSize { get; set; }

        public string SortExpression { get; set; }
    }
}