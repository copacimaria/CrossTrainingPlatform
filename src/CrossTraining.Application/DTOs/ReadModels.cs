using CrossTraining.Application.Authorization;

namespace CrossTraining.Application.DTOs;

// Modele de citire intoarse de stratul Application. Sunt plate si imutabile:
// tot ce iese din Application e o copie, nu o entitate urmarita de EF Core.

/// <summary>Un drept, asa cum apare in listele de administrare.</summary>
public sealed record ClaimDto(int ClaimId, string ClaimName, bool IsActive)
{
    /// <summary>Drept verificat de cod: nu poate fi redenumit sau dezactivat.</summary>
    public bool IsSystem => Permissions.IsSystemPermission(ClaimName);
}

/// <summary>Un drept impreuna cu rolurile care il au.</summary>
public sealed record ClaimWithRolesDto(int ClaimId, string ClaimName, IReadOnlyList<string> RoleNames);

/// <summary>Un rol de utilizator.</summary>
public sealed record RoleDto(int RoleId, string RoleName, bool IsActive)
{
    /// <summary>Rol de care depinde aplicatia: nu poate fi redenumit sau dezactivat.</summary>
    public bool IsSystem => SystemRoles.IsSystemRole(RoleName);
}

/// <summary>Un domeniu de antrenament.</summary>
public sealed record DomainDto(int DomainId, string DomainName, bool IsActive);

/// <summary>Un joc, cu numele domeniului deja rezolvat.</summary>
public sealed record GameDto(
    int GameId,
    string GameName,
    string? Description,
    string? DownloadLink,
    bool IsActive,
    int? DomainId,
    string? DomainName);

/// <summary>Jocurile vizibile unui utilizator: toate, plus cele din domeniile lui.</summary>
public sealed record GamesForUserDto(
    IReadOnlyList<GameDto> AllGames,
    IReadOnlyList<GameDto> GamesForSelectedDomains);

/// <summary>Datele de profil, pregatite pentru afisare.</summary>
public sealed record UserProfileDto(string FullName, string Email, string Role, string Domains);

/// <summary>
/// Utilizatorul autentificat, cat trebuie ca sa se construiasca cookie-ul.
/// Intentionat nu expune hash-ul parolei si nici entitatea completa.
/// </summary>
public sealed record AuthenticatedUserDto(int UserId, string FullName, string Email);

/// <summary>
/// Datele de securitate ale unui utilizator, folosite la construirea si la
/// reimprospatarea cookie-ului de autentificare.
/// </summary>
public sealed record UserSecurityContextDto(
    int UserId,
    string FullName,
    string Email,
    string? RoleName,
    IReadOnlyList<string> Permissions);
