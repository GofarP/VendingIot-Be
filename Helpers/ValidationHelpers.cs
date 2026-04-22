using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace VendingIot.Helpers;

public class Validation
{
    public static void Required(ModelStateDictionary modelState, string key, object? value, string pesan = "Field ini wajib diisi!")
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
        {
            modelState.AddModelError(key, pesan);
        }
    }

    public static async Task Unique<T>(ModelStateDictionary modelState, IQueryable<T> dbSet, Expression<Func<T, bool>> predicate, string key, string message) where T : class
    {
        var isExist = await dbSet.AnyAsync(predicate);
        if (isExist)
        {
            modelState.AddModelError(key, message);
        }
    }

    public static void Max(ModelStateDictionary modelState, string key, string? value, int limit, string message)
    {
        if (value?.Length > limit)
        {
            modelState.AddModelError(key, message);
        }
    }
}