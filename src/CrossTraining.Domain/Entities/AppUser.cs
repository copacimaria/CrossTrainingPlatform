using CrossTraining.Domain.Common;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.Domain.Entities;

/// <summary>
/// Utilizator al platformei. Un user poate avea mai multe domenii (many-to-many prin UserDomain).
///
/// Parola se stocheaza doar ca hash; calculul lui apartine infrastructurii (Argon2id),
/// dar regula de lungime minima apartine domeniului (<see cref="ValidatePassword"/>).
/// </summary>
public class AppUser : ISoftDeletable
{
    public const int NameMaxLength = 100;
    public const int PasswordMinLength = 6;

    /// <summary>Folosit de EF Core la materializare.</summary>
    private AppUser()
    {
    }

    public int UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FamilyName { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;

    /// <summary>Hash-ul parolei (Argon2id, format PHC). Niciodata text clar.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime? CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public int? RoleId { get; private set; }
    public AppRole? Role { get; private set; }

    /// <summary>
    /// Legaturile catre domenii (many-to-many). Pentru <see cref="ChooseDomains"/>, colectia
    /// trebuie incarcata complet, inclusiv legaturile catre domenii dezactivate.
    /// </summary>
    public ICollection<UserDomain> UserDomains { get; private set; } = new List<UserDomain>();

    public string FullName => $"{Name} {FamilyName}";

    /// <summary>Un cont nou, activ, cu rolul primit la inregistrare. Parola se seteaza separat, ca hash.</summary>
    public static DomainResult<AppUser> Register(
        string name,
        string familyName,
        Email email,
        int roleId,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(email);

        var validName = ValidatePersonName(name, "Prenumele");
        if (!validName.Succeeded)
            return DomainResult.Fail<AppUser>(validName.Error!);

        var validFamilyName = ValidatePersonName(familyName, "Numele de familie");
        if (!validFamilyName.Succeeded)
            return DomainResult.Fail<AppUser>(validFamilyName.Error!);

        return DomainResult.Ok(new AppUser
        {
            Name = validName.Value,
            FamilyName = validFamilyName.Value,
            Email = email,
            RoleId = roleId,
            CreatedAt = createdAt,
            IsActive = true
        });
    }

    /// <summary>Regula pentru o parola noua, verificata inainte de calculul hash-ului.</summary>
    public static DomainResult ValidatePassword(string? plainPassword)
        => string.IsNullOrEmpty(plainPassword) || plainPassword.Length < PasswordMinLength
            ? DomainResult.Fail($"Parola trebuie sa aiba minim {PasswordMinLength} caractere.")
            : DomainResult.Ok();

    /// <summary>Inlocuieste hash-ul parolei. Primeste doar hash-ul, niciodata parola in clar.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Inlocuieste lista de domenii. Utilizatorul trebuie sa aiba cel putin unul: fara
    /// domenii, nu primeste niciun joc. Existenta domeniilor o verifica apelantul, care
    /// are acces la date.
    /// </summary>
    public DomainResult ChooseDomains(IEnumerable<int> domainIds)
    {
        ArgumentNullException.ThrowIfNull(domainIds);

        var chosen = domainIds.Distinct().ToHashSet();
        if (chosen.Count == 0)
            return DomainResult.Fail("Selecteaza cel putin un domeniu.");

        // Legaturile scoase se sterg ca orfani; cele noi se adauga. Cele care raman nu se
        // ating, deci nu apar operatii inutile in baza de date.
        foreach (var link in UserDomains.Where(ud => !chosen.Contains(ud.DomainId)).ToList())
            UserDomains.Remove(link);

        foreach (var domainId in chosen.Where(id => UserDomains.All(ud => ud.DomainId != id)))
            UserDomains.Add(new UserDomain(UserId, domainId));

        return DomainResult.Ok();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private static DomainResult<string> ValidatePersonName(string? value, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            return DomainResult.Fail<string>($"{field} este obligatoriu.");

        if (trimmed.Length > NameMaxLength)
            return DomainResult.Fail<string>($"{field} poate avea cel mult {NameMaxLength} de caractere.");

        return DomainResult.Ok(trimmed);
    }
}
