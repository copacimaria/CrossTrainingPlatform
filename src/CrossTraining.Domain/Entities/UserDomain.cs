namespace CrossTraining.Domain.Entities;

/// <summary>
/// Tabel de legatura many-to-many intre AppUser si Domain.
/// Inlocuieste vechea coloana JSON "DomainList".
///
/// Se creeaza prin <see cref="AppUser.ChooseDomains"/>, nu direct.
/// </summary>
public class UserDomain
{
    /// <summary>Folosit de EF Core la materializare.</summary>
    private UserDomain()
    {
    }

    public UserDomain(int userId, int domainId)
    {
        UserId = userId;
        DomainId = domainId;
    }

    public int UserId { get; private set; }
    public AppUser? User { get; private set; }

    public int DomainId { get; private set; }
    public TrainingDomain? Domain { get; private set; }
}
