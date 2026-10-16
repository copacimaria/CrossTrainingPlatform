using System.Globalization;
using System.Security.Claims;
using CrossTraining.Web.Security;

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
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    /// <summary>Numele complet stocat la login.</summary>
    public static string GetFullName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    /// <summary>
    /// Are utilizatorul dreptul numit?
    ///
    /// Aceeasi regula o aplica si <see cref="PermissionAuthorizationHandler"/>, deci un
    /// buton afisat intr-un view duce la o actiune pe care utilizatorul chiar o poate face.
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
        => user.HasClaim(c => c.Type == PermissionClaim.Type
                           && string.Equals(c.Value, permission, StringComparison.Ordinal));
}
