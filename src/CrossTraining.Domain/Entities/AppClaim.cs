using CrossTraining.Domain.Authorization;
using CrossTraining.Domain.Common;

namespace CrossTraining.Domain.Entities;

/// <summary>
/// Permisiune atomica ce poate fi atasata unui rol.
///
/// Drepturile verificate de cod (<see cref="Permissions"/>) sunt protejate aici, in
/// entitate: nu pot fi redenumite sau dezactivate, iar numele lor nu poate fi luat de alt
/// drept. Regula se aplica oricine ar apela entitatea - un caz de utilizare nou nu o poate
/// ocoli uitand o verificare.
/// </summary>
public class AppClaim : ISoftDeletable
{
    public const int NameMaxLength = 100;

    /// <summary>Folosit de EF Core la materializare.</summary>
    private AppClaim()
    {
    }

    public int ClaimId { get; private set; }
    public string ClaimName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>Legaturile catre roluri (many-to-many prin RoleClaim).</summary>
    public ICollection<RoleClaim> RoleClaims { get; private set; } = new List<RoleClaim>();

    /// <summary>Drept verificat de cod: nu poate fi redenumit sau dezactivat.</summary>
    public bool IsSystem => Permissions.IsSystemPermission(ClaimName);

    /// <summary>
    /// Un drept nou, activ. Drepturile de sistem nu se creeaza de aici: le insereaza
    /// migrarea SeedPermissions, impreuna cu acordarea lor catre rolul Admin.
    /// </summary>
    public static DomainResult<AppClaim> Create(string name)
    {
        var validName = ValidateName(name);
        if (!validName.Succeeded)
            return DomainResult.Fail<AppClaim>(validName.Error!);

        return DomainResult.Ok(new AppClaim { ClaimName = validName.Value, IsActive = true });
    }

    public DomainResult Rename(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        // Salvarea formularului fara nicio schimbare nu este o redenumire.
        if (string.Equals(ClaimName, trimmed, StringComparison.Ordinal))
            return DomainResult.Ok();

        // Autorizarea cauta drepturile dupa nume: un drept de sistem redenumit inceteaza
        // sa mai deschida ecranele pe care le protejeaza.
        if (IsSystem)
            return DomainResult.Fail($"Dreptul \"{ClaimName}\" este folosit de aplicatie si nu poate fi redenumit.");

        var validName = ValidateName(trimmed);
        if (!validName.Succeeded)
            return validName;

        ClaimName = validName.Value;
        return DomainResult.Ok();
    }

    public DomainResult Deactivate()
    {
        // Un drept de sistem dezactivat dispare din toate rolurile (filtrul global), deci
        // ecranele protejate de el devin inaccesibile pentru toata lumea.
        if (IsSystem)
            return DomainResult.Fail($"Dreptul \"{ClaimName}\" este folosit de aplicatie si nu poate fi dezactivat.");

        IsActive = false;
        return DomainResult.Ok();
    }

    /// <summary>Rolurile care aveau dreptul il primesc inapoi: legaturile nu s-au sters.</summary>
    public void Reactivate() => IsActive = true;

    /// <summary>
    /// Numele validat, fara spatii la capete. Numele drepturilor de sistem este rezervat:
    /// un drept oarecare, deja acordat unui rol, redenumit in "roles.manage" i-ar da acelui
    /// rol administrarea tuturor drepturilor.
    /// </summary>
    private static DomainResult<string> ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            return DomainResult.Fail<string>("Numele dreptului este obligatoriu.");

        if (trimmed.Length > NameMaxLength)
            return DomainResult.Fail<string>($"Numele dreptului poate avea cel mult {NameMaxLength} de caractere.");

        if (Permissions.IsSystemPermission(trimmed))
            return DomainResult.Fail<string>($"Numele \"{trimmed}\" este rezervat unui drept al aplicatiei.");

        return DomainResult.Ok(trimmed);
    }
}
