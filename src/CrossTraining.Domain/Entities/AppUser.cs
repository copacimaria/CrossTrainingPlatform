namespace CrossTraining.Domain.Entities;

/// <summary>
/// Utilizator al platformei. Un user poate avea mai multe domenii (many-to-many prin UserDomain).
/// Parola este stocata doar ca hash (vezi Infrastructure/PasswordService).
/// </summary>
public class AppUser
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash-ul parolei (PBKDF2). Niciodata text clar.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime? CreationDate { get; set; }
    public bool IsActive { get; set; } = true;

    public int? RoleId { get; set; }
    public AppRole? Role { get; set; }

    /// <summary>Legaturile catre domenii (many-to-many).</summary>
    public ICollection<UserDomain> UserDomains { get; set; } = new List<UserDomain>();
}
