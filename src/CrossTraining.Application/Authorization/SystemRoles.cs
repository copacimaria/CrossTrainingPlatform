namespace CrossTraining.Application.Authorization;

/// <summary>
/// Rolurile de care depinde functionarea aplicatiei, create de migrarea SeedPermissions.
///
/// Nu pot fi redenumite si nu pot fi dezactivate din ecranele de administrare:
/// <list type="bullet">
///   <item><see cref="User"/> este rolul atribuit fiecarui cont nou.</item>
///   <item><see cref="Admin"/> detine drepturile de administrare. Dezactivarea lui, sau
///   scoaterea unui drept de sistem din el, ar lasa aplicatia fara niciun administrator,
///   iar refacerea s-ar putea face doar direct in baza de date.</item>
/// </list>
///
/// Nu este o scurtatura de autorizare: rolul Admin nu primeste nimic din cod. Drepturile
/// lui vin tot din baza de date; aici se impiedica doar pierderea lor din greseala.
/// </summary>
public static class SystemRoles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static IReadOnlyList<string> All { get; } = [User, Admin];

    /// <summary>Fara diferenta intre litere mari si mici, ca indexul unic de pe <c>RoleName</c>.</summary>
    public static bool IsSystemRole(string roleName)
        => All.Contains(roleName.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsAdmin(string roleName)
        => string.Equals(roleName.Trim(), Admin, StringComparison.OrdinalIgnoreCase);
}
