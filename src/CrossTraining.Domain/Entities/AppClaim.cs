namespace CrossTraining.Domain.Entities;

/// <summary>Permisiune atomica ce poate fi atasata unui rol.</summary>
public class AppClaim : ISoftDeletable
{
    public int ClaimId { get; set; }
    public string ClaimName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Legaturile catre roluri (many-to-many prin RoleClaim).</summary>
    public ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();
}
