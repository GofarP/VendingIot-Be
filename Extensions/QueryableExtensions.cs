using Microsoft.EntityFrameworkCore;
using VendingIot.Models.ApiResponse;

namespace VendingIot.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        string? message = null,
        bool? success = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<T>(data, totalCount, page, pageSize, message, success);
    }

    public static PagedResponse<T> ToPagedResponse<T>(
        this IEnumerable<T> data,
        int totalCount,
        int page,
        int pageSize,
        string? message = null,
        bool? success = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        return new PagedResponse<T>(data, totalCount, page, pageSize, message, success);
    }
}
