using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Common.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Synchronous in-memory paging helper (used by tests and non-EF collections).
    /// EF queries page asynchronously in the services themselves.
    /// </summary>
    public static PaginationResponse<T> ToPagedList<T>(
        this IEnumerable<T> source, PaginationRequest request)
    {
        var list = source.ToList();
        var items = list.Skip(request.Skip).Take(request.PageSize).ToList();
        return new PaginationResponse<T>(items, list.Count, request.Page, request.PageSize);
    }
}
