namespace CrossTraining.Domain.Entities;

/// <summary>
/// Tabel de legatura many-to-many intre AppRole si AppClaim.
/// Corespunde tabelei existente dbo.AppClaim_AppRole.
///
/// Se creeaza prin <see cref="AppRole.Grant"/>, nu direct: rolul aplica regulile acordarii.
/// </summary>
public class RoleClaim
{
    /// <summary>Folosit de EF Core la materializare.</summary>
    private RoleClaim()
    {
    }

    public RoleClaim(int roleId, int claimId)
    {
        RoleId = roleId;
        ClaimId = claimId;
    }

    public int RoleId { get; private set; }
    public AppRole? Role { get; private set; }

    public int ClaimId { get; private set; }
    public AppClaim? Claim { get; private set; }
}
