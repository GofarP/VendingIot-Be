using System.Text.Json.Serialization;

namespace VendingIot.Models.ApiResponse;

public class PaginationMeta
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class PagedResponse<T>
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    public IEnumerable<T> Data { get; set; } = [];

    public PaginationMeta Pagination { get; set; } = new();

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> data, int totalCount, int currentPage, int pageSize, string? message = null, bool? success = null)
    {
        Data = data;
        Message = message;
        Success = success;
        Pagination = new PaginationMeta
        {
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}