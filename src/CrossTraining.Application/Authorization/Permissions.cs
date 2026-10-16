namespace CrossTraining.Application.Authorization;

/// <summary>
/// Numele drepturilor, asa cum sunt stocate in <c>dbo.AppClaim</c>.
///
/// Sunt constante, nu string-uri libere prin controllere: o greseala de scriere ar face
/// endpointul inaccesibil pentru toata lumea, iar asta se observa greu.
/// Orice adaugare aici trebuie insotita de un rand in baza de date - vezi migrarea
/// SeedPermissions.
///
/// Stau in Application, nu in Web: pe langa atributele de autorizare din controllere, le
/// folosesc si cazurile de utilizare, ca sa refuze redenumirea sau dezactivarea unui drept
/// de care depinde codul.
/// </summary>
public static class Permissions
{
    public static class Claims
    {
        public const string View = "claims.view";
        public const string Manage = "claims.manage";
    }

    public static class Roles
    {
        public const string View = "roles.view";
        public const string Manage = "roles.manage";
    }

    public static class Domains
    {
        public const string View = "domains.view";
        public const string Manage = "domains.manage";
    }

    public static class Games
    {
        public const string Manage = "games.manage";
    }

    /// <summary>Toate drepturile cunoscute, in ordinea in care sunt inserate in baza.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Claims.View,
        Claims.Manage,
        Roles.View,
        Roles.Manage,
        Domains.View,
        Domains.Manage,
        Games.Manage
    ];

    /// <summary>
    /// Este numele unui drept verificat de cod?
    ///
    /// Fara diferenta intre litere mari si mici, ca in baza de date: indexul unic de pe
    /// <c>ClaimName</c> trateaza "Roles.Manage" si "roles.manage" ca acelasi nume.
    /// </summary>
    public static bool IsSystemPermission(string claimName)
        => All.Contains(claimName.Trim(), StringComparer.OrdinalIgnoreCase);
}
