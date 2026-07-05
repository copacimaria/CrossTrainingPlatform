namespace CrossTraining.Domain.Entities;

/// <summary>
/// Tabel de legatura many-to-many intre AppRole si AppClaim.
/// Corespunde tabelei existente dbo.AppClaim_AppRole.
/// </summary>
public class RoleClaim
{
    public int RoleId { get; set; }
    public AppRole? Role { get; set; }

    public int ClaimId { get; set; }
    public AppClaim? Claim { get; set; }
}
