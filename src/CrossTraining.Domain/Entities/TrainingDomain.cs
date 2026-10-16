using CrossTraining.Domain.Common;

namespace CrossTraining.Domain.Entities;

/// <summary>Domeniu de antrenament (ex: matematica, limbi straine).</summary>
public class TrainingDomain : ISoftDeletable
{
    public const int NameMaxLength = 150;

    /// <summary>Folosit de EF Core la materializare.</summary>
    private TrainingDomain()
    {
    }

    public int DomainId { get; private set; }
    public string DomainName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>Jocurile care apartin acestui domeniu (un domeniu are mai multe jocuri).</summary>
    public ICollection<Game> Games { get; private set; } = new List<Game>();

    /// <summary>Legaturile catre utilizatori (many-to-many).</summary>
    public ICollection<UserDomain> UserDomains { get; private set; } = new List<UserDomain>();

    public static DomainResult<TrainingDomain> Create(string name, bool isActive)
    {
        var validName = ValidateName(name);
        if (!validName.Succeeded)
            return DomainResult.Fail<TrainingDomain>(validName.Error!);

        return DomainResult.Ok(new TrainingDomain { DomainName = validName.Value, IsActive = isActive });
    }

    public DomainResult Rename(string name)
    {
        var validName = ValidateName(name);
        if (!validName.Succeeded)
            return validName;

        DomainName = validName.Value;
        return DomainResult.Ok();
    }

    /// <summary>Starea aleasa in formularul de editare; domeniile nu au restrictii de dezactivare.</summary>
    public void SetActive(bool isActive) => IsActive = isActive;

    public void Deactivate() => IsActive = false;

    private static DomainResult<string> ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            return DomainResult.Fail<string>("Numele domeniului este obligatoriu.");

        if (trimmed.Length > NameMaxLength)
            return DomainResult.Fail<string>($"Numele domeniului poate avea cel mult {NameMaxLength} de caractere.");

        return DomainResult.Ok(trimmed);
    }
}
