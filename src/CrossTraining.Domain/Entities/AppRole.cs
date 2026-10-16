using CrossTraining.Domain.Authorization;
using CrossTraining.Domain.Common;

namespace CrossTraining.Domain.Entities;

/// <summary>
/// Rol de utilizator (ex: Admin, User). Are asociate mai multe drepturi.
///
/// Rolul este radacina legaturilor rol-drept: acordarea si revocarea trec prin el, ca sa
/// poata aplica regula ca rolul Admin nu pierde niciodata un drept de sistem.
/// </summary>
public class AppRole : ISoftDeletable
{
    public const int NameMaxLength = 100;

    /// <summary>Folosit de EF Core la materializare.</summary>
    private AppRole()
    {
    }

    public int RoleId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public ICollection<AppUser> Users { get; private set; } = new List<AppUser>();

    /// <summary>
    /// Legaturile catre drepturi (many-to-many prin RoleClaim). Pentru <see cref="Grant"/>
    /// si <see cref="Revoke"/>, colectia trebuie incarcata complet, inclusiv legaturile catre
    /// drepturi dezactivate.
    /// </summary>
    public ICollection<RoleClaim> RoleClaims { get; private set; } = new List<RoleClaim>();

    /// <summary>Rol de care depinde aplicatia: nu poate fi redenumit sau dezactivat.</summary>
    public bool IsSystem => SystemRoles.IsSystemRole(RoleName);

    public bool IsAdmin => SystemRoles.IsAdmin(RoleName);

    public static DomainResult<AppRole> Create(string name)
    {
        var validName = ValidateName(name);
        if (!validName.Succeeded)
            return DomainResult.Fail<AppRole>(validName.Error!);

        return DomainResult.Ok(new AppRole { RoleName = validName.Value, IsActive = true });
    }

    public DomainResult Rename(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (string.Equals(RoleName, trimmed, StringComparison.Ordinal))
            return DomainResult.Ok();

        // Rolurile de sistem sunt recunoscute dupa nume, deci unul redenumit si-ar pierde
        // protectia.
        if (IsSystem)
            return DomainResult.Fail($"Rolul \"{RoleName}\" este folosit de aplicatie si nu poate fi redenumit.");

        var validName = ValidateName(trimmed);
        if (!validName.Succeeded)
            return validName;

        RoleName = validName.Value;
        return DomainResult.Ok();
    }

    public DomainResult Deactivate()
    {
        // Un rol dezactivat isi pierde toate drepturile (filtrul global). Pentru Admin,
        // nimeni nu ar mai putea administra aplicatia; pentru User, fiecare cont nou ar
        // porni dintr-un rol inexistent.
        if (IsSystem)
            return DomainResult.Fail($"Rolul \"{RoleName}\" este folosit de aplicatie si nu poate fi dezactivat.");

        IsActive = false;
        return DomainResult.Ok();
    }

    /// <summary>Rolul isi recapata drepturile: legaturile nu s-au sters la dezactivare.</summary>
    public void Reactivate() => IsActive = true;

    /// <summary>Acorda dreptul. Idempotent: acordarea unui drept deja acordat nu este o eroare.</summary>
    public DomainResult Grant(AppClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        if (!claim.IsActive)
            return DomainResult.Fail("Dreptul nu exista sau este dezactivat.");

        if (RoleClaims.Any(rc => rc.ClaimId == claim.ClaimId))
            return DomainResult.Ok();

        RoleClaims.Add(new RoleClaim(RoleId, claim.ClaimId));
        return DomainResult.Ok();
    }

    /// <summary>
    /// Revoca dreptul. Idempotent: revocarea unui drept pe care rolul nu il are nu este o eroare.
    ///
    /// Rolul Admin pastreaza drepturile de sistem: fara roles.manage, de exemplu, nimeni nu
    /// ar mai putea acorda inapoi dreptul scos - nici macar adminul care l-a scos.
    /// </summary>
    public DomainResult Revoke(AppClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var link = RoleClaims.FirstOrDefault(rc => rc.ClaimId == claim.ClaimId);
        if (link == null)
            return DomainResult.Ok();

        if (IsAdmin && claim.IsSystem)
        {
            return DomainResult.Fail(
                $"Dreptul \"{claim.ClaimName}\" nu poate fi scos din rolul {SystemRoles.Admin}: " +
                "aplicatia ar ramane fara cine sa o administreze.");
        }

        RoleClaims.Remove(link);
        return DomainResult.Ok();
    }

    private static DomainResult<string> ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            return DomainResult.Fail<string>("Numele rolului este obligatoriu.");

        if (trimmed.Length > NameMaxLength)
            return DomainResult.Fail<string>($"Numele rolului poate avea cel mult {NameMaxLength} de caractere.");

        if (SystemRoles.IsSystemRole(trimmed))
            return DomainResult.Fail<string>($"Numele \"{trimmed}\" este rezervat unui rol al aplicatiei.");

        return DomainResult.Ok(trimmed);
    }
}
