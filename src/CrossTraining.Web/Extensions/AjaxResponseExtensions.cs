using CrossTraining.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Extensions;

/// <summary>
/// Raspunsurile actiunilor apelate din JavaScript, intr-o singura forma.
///
/// Reusita: <c>200 { success: true }</c>. Orice esec: un <see cref="ProblemDetails"/> standard,
/// cu codul HTTP potrivit si cu campurile pe care le citeste JS-ul (<c>success</c>,
/// <c>errors</c>), plus <c>traceId</c>:
/// <list type="bullet">
///   <item>400 - formular invalid sau operatie refuzata de o regula de business;</item>
///   <item>404 - inregistrarea ceruta nu exista;</item>
///   <item>500 - eroare neasteptata (vezi <c>AjaxExceptionHandler</c>, aceeasi forma).</item>
/// </list>
///
/// Inainte, un refuz pleca cu 200 si <c>{ success: false }</c>, iar o inregistrare lipsa cu
/// 200 si un camp <c>message</c> pe care JS-ul nu il citea.
/// </summary>
public static class AjaxResponseExtensions
{
    /// <summary>Rezultatul unei comenzi: 200 la reusita, 400 cu motivul la refuz.</summary>
    public static IActionResult AjaxOutcome(this ControllerBase controller, OperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Succeeded
            ? new JsonResult(new { success = true })
            : controller.AjaxProblem(StatusCodes.Status400BadRequest, "Operatia a fost refuzata.", [result.Error!]);
    }

    /// <summary>Formularul nu a trecut validarea: 400 cu mesajele de langa campuri.</summary>
    public static IActionResult AjaxInvalidForm(this ControllerBase controller)
        => controller.AjaxProblem(
            StatusCodes.Status400BadRequest,
            "Datele trimise nu sunt valide.",
            controller.ModelState.ErrorMessages());

    /// <summary>Inregistrarea ceruta nu exista: 404, cu un mesaj pentru utilizator.</summary>
    public static IActionResult AjaxNotFound(this ControllerBase controller, string message)
        => controller.AjaxProblem(StatusCodes.Status404NotFound, "Inregistrarea nu exista.", [message]);

    private static ObjectResult AjaxProblem(
        this ControllerBase controller,
        int statusCode,
        string title,
        IReadOnlyCollection<string> errors)
    {
        // Fabrica din MVC completeaza tipul, titlul si traceId-ul, la fel ca pentru orice
        // ProblemDetails din aplicatie.
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode,
            title);

        problem.Extensions["success"] = false;
        problem.Extensions["errors"] = errors;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
