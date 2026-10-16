using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CrossTraining.Web.Extensions;

public static class ModelStateExtensions
{
    /// <summary>
    /// Mesajele de validare, in forma pe care o asteapta JS-ul din paginile de administrare.
    /// Erau duplicate ca metoda privata in fiecare controller.
    /// </summary>
    public static string[] ErrorMessages(this ModelStateDictionary modelState)
        => modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToArray();
}
