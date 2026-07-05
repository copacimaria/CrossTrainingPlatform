namespace CrossTraining.Domain.Entities;

/// <summary>Domeniu de antrenament (ex: matematica, limbi straine).</summary>
public class TrainingDomain
{
    public int DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Jocurile care apartin acestui domeniu (un domeniu are mai multe jocuri).</summary>
    public ICollection<Game> Games { get; set; } = new List<Game>();

    /// <summary>Legaturile catre utilizatori (many-to-many).</summary>
    public ICollection<UserDomain> UserDomains { get; set; } = new List<UserDomain>();
}
