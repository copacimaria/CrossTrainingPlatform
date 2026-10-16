using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IAppClaimRepository
{
    /// <summary>
    /// O pagina de drepturi pentru ecranul de administrare, inclusiv cele dezactivate.
    /// Filtrarea, numararea si decuparea se fac in baza de date.
    /// </summary>
    Task<PagedResult<AppClaim>> SearchForAdminAsync(string? searchTerm, int page, int pageSize);

    /// <summary>Dreptul impreuna cu rolurile care il au. Include si drepturile dezactivate.</summary>
    Task<AppClaim?> GetForAdminAsync(int id);

    /// <summary>
    /// Dreptul cu acest nume, activ sau nu. Comparatia o face baza de date, cu aceleasi
    /// reguli ca indexul unic (fara diferenta intre litere mari si mici).
    /// </summary>
    Task<AppClaim?> GetByNameForAdminAsync(string claimName);

    /// <summary>Drepturile active care NU sunt deja atasate rolului, filtrate dupa un termen.</summary>
    Task<List<AppClaim>> SearchAvailableForRoleAsync(int roleId, string? searchTerm, int limit);

    void Add(AppClaim claim);
}
