using CrossTraining.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CrossTraining.Web.Extensions;

public static class OperationResultExtensions
{
    /// <summary>
    /// Raspunsul JSON in forma pe care o asteapta JS-ul din paginile de administrare:
    /// <c>{ success }</c> sau <c>{ success: false, errors: [...] }</c>.
    ///
    /// Inainte, actiunile raspundeau <c>success: true</c> indiferent de ce intorcea
    /// comanda, deci o operatie refuzata aparea in ecran ca reusita.
    /// </summary>
    public static JsonResult ToJsonResult(this OperationResult result)
        => result.Succeeded
            ? new JsonResult(new { success = true })
            : new JsonResult(new { success = false, errors = new[] { result.Error } });
}
