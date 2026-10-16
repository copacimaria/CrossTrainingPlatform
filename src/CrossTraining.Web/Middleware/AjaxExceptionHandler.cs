using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Middleware;

/// <summary>
/// Raspunsul la o eroare neasteptata, pentru cererile trimise din JavaScript.
///
/// Paginile de administrare lucreaza prin fetch si asteapta JSON. Inainte, o exceptie
/// ajungea la ele ca pagina HTML de eroare, pe care JS-ul nu o putea citi, deci
/// utilizatorul vedea "Eroare necunoscuta". Acum primesc un ProblemDetails standard,
/// care contine si campurile pe care le citeste JS-ul (<c>success</c>, <c>errors</c>),
/// plus codul cererii, ca eroarea sa poata fi gasita in log.
///
/// Cererile obisnuite de pagina nu sunt tratate aici: middleware-ul de exceptii le trimite
/// mai departe la /Home/Error. Exceptia o logheaza deja middleware-ul, o singura data.
/// </summary>
public sealed class AjaxExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public AjaxExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!IsAjax(httpContext.Request))
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Acelasi cod ca pe pagina de eroare (HomeController.Error), ca sa se poata cauta
        // in log indiferent de unde l-a raportat utilizatorul.
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "A aparut o eroare neasteptata.",
                Extensions =
                {
                    ["traceId"] = traceId,
                    ["success"] = false,
                    ["errors"] = new[] { $"A aparut o eroare neasteptata. Codul pentru suport: {traceId}" }
                }
            }
        });
    }

    /// <summary>
    /// JS-ul din paginile de administrare trimite token-ul antiforgery ca header la fiecare
    /// POST; cererile care cer explicit JSON sunt tratate la fel.
    /// </summary>
    private static bool IsAjax(HttpRequest request)
        => request.Headers.ContainsKey("RequestVerificationToken")
           || request.Headers.XRequestedWith == "XMLHttpRequest"
           || request.Headers.Accept.Any(value =>
                  value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
