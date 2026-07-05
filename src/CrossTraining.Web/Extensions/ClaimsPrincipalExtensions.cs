using System.Security.Claims;

namespace CrossTraining.Web.Extensions;

/// <summary>
/// Helpers pentru citirea datelor utilizatorului autentificat din claims-urile cookie-ului.
/// Inlocuieste vechiul mod bazat pe HttpContext.Session.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Id-ul utilizatorului curent, sau null daca nu e autentificat.</summary>
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>Numele complet stocat la login.</summary>
    public static string GetFullName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
