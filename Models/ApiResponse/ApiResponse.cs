public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; }

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public PagedResponse(IEnumerable<T> data, int count, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = count;
        CurrentPage = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }
}