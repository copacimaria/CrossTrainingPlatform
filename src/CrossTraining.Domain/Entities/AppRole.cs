namespace CrossTraining.Domain.Entities;

/// <summary>Rol de utilizator (ex: Admin, User). Are asociate mai multe claim-uri.</summary>
public class AppRole : ISoftDeletable
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();

    /// <summary>Legaturile catre claim-uri (many-to-many prin RoleClaim).</summary>
    public ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();
}
