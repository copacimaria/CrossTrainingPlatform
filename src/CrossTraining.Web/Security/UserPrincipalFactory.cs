using System.Globalization;
using System.Security.Claims;
using CrossTraining.Application.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CrossTraining.Web.Security;

/// <summary>
/// Construieste identitatea din cookie dintr-un context de securitate incarcat din baza.
///
/// Este singurul loc care stie ce claim-uri intra in cookie, ca sa nu apara divergente
/// intre login si reimprospatare: daca ele ar construi identitati diferite, o sesiune
/// reimprospatata ar putea pierde tacut un claim.
/// </summary>
public static class UserPrincipalFactory
{
    /// <summary>Momentul la care claim-urile au fost citite din baza de date.</summary>
    public const string RefreshedAtClaim = "refreshed_at";

    public static ClaimsPrincipal Create(UserSecurityContextDto context, DateTimeOffset refreshedAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, context.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, context.FullName),
            new(ClaimTypes.Email, context.Email),
            new(RefreshedAtClaim, refreshedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
        };

        if (!string.IsNullOrEmpty(context.RoleName))
            claims.Add(new Claim(ClaimTypes.Role, context.RoleName));

        foreach (var permission in context.Permissions)
            claims.Add(new Claim(PermissionClaim.Type, permission));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Cand au fost citite ultima data claim-urile, sau null daca nu se stie.</summary>
    public static DateTimeOffset? ReadRefreshedAt(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(RefreshedAtClaim);

        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}
