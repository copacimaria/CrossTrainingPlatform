using System.ComponentModel.DataAnnotations;
using CrossTraining.Application.DTOs;
using X.PagedList;

namespace CrossTraining.Web.Models;

// Lungimile reflecta schema din db/002_Recreate_Schema.sql. Cand intra migrarile EF
// (etapa 5), sursa de adevar devine configurarea si acestea se aliniaza dupa ea.

public class ClaimFormModel
{
    public int ClaimId { get; set; }

    [Required(ErrorMessage = "Claim-ul trebuie sa aiba un nume!")]
    [StringLength(100)]
    public string ClaimName { get; set; } = string.Empty;
}

public class RoleFormModel
{
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Rolul trebuie sa aibe un nume!")]
    [StringLength(100)]
    public string RoleName { get; set; } = string.Empty;
}

public class DomainFormModel
{
    public int DomainId { get; set; }

    [Required, StringLength(150)]
    public string DomainName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;


    public static DomainFormModel FromDto(DomainDto dto)
        => new() { DomainId = dto.DomainId, DomainName = dto.DomainName, IsActive = dto.IsActive };
}

public class GameFormModel
{
    public int GameId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Nume joc")]
    public string GameName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Descriere")]
    public string Description { get; set; } = string.Empty;

    // Fara [Url]: baza contine deja linkuri in forme variate, iar o regula noua de
    // validare ar bloca salvarea unor jocuri care se editau pana acum fara probleme.
    [StringLength(500)]
    [Display(Name = "Link de descarcare")]
    public string? DownloadLink { get; set; }

    [Display(Name = "Domain")]
    public int? DomainId { get; set; }


    public static GameFormModel FromDto(GameDto dto) => new()
    {
        GameId = dto.GameId,
        GameName = dto.GameName,
        Description = dto.Description ?? string.Empty,
        DownloadLink = dto.DownloadLink,
        DomainId = dto.DomainId
    };
}

/// <summary>Ecranul de detalii al unui rol: rolul plus drepturile lui, paginate.</summary>
public class RoleClaimsViewModel
{
    public RoleDto Role { get; set; } = new(0, string.Empty, false);
    public IPagedList<ClaimDto>? Claims { get; set; }
}
