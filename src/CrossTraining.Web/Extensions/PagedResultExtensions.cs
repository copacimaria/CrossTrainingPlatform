using CrossTraining.Application.DTOs;
using X.PagedList;

namespace CrossTraining.Web.Extensions;

/// <summary>
/// Adapteaza rezultatul paginat al stratului Application la tipul cerut de componenta
/// de paginare din view-uri.
///
/// <c>StaticPagedList</c> primeste doar randurile paginii curente plus numarul total,
/// deci paginarea ramane facuta in baza de date - biblioteca de UI nu mai vede niciodata
/// setul complet.
/// </summary>
public static class PagedResultExtensions
{
    public static IPagedList<T> ToPagedList<T>(this PagedResult<T> result)
        => new StaticPagedList<T>(result.Items, result.Page, result.PageSize, result.TotalCount);
}
