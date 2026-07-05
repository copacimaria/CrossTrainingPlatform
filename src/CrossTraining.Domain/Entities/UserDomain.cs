namespace CrossTraining.Domain.Entities;

/// <summary>
/// Tabel de legatura many-to-many intre AppUser si Domain.
/// Inlocuieste vechea coloana JSON "DomainList".
/// </summary>
public class UserDomain
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int DomainId { get; set; }
    public TrainingDomain? Domain { get; set; }
}
