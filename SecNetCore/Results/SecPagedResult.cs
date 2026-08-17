namespace SecNetCore.Results
{
    public class SecPagedResult<T>
    {
        public List<T> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages =>
            PageSize <= 0
                ? 0
                : (int)Math.Ceiling(
                    TotalRecords / (double)PageSize);
    }
}
