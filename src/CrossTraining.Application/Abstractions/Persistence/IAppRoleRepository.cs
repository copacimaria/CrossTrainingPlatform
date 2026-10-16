using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IAppRoleRepository
{
    /// <summary>
    /// O pagina de roluri pentru ecranul de administrare, inclusiv cele dezactivate -
    /// altfel un rol dezactivat nu ar mai putea fi gasit ca sa fie reactivat.
    /// </summary>
    Task<PagedResult<AppRole>> SearchForAdminAsync(string? searchTerm, int page, int pageSize);

    Task<AppRole?> GetByIdAsync(int id);

    /// <summary>Rolul, inclusiv daca este dezactivat. Necesar ca sa poata fi reactivat.</summary>
    Task<AppRole?> GetForAdminAsync(int id);

    /// <summary>
    /// Rolul (activ sau nu), impreuna cu toate legaturile lui catre drepturi - inclusiv cele
    /// catre drepturi dezactivate. Necesar pentru <see cref="AppRole.Grant"/> si
    /// <see cref="AppRole.Revoke"/>, care lucreaza pe colectia de legaturi.
    /// </summary>
    Task<AppRole?> GetWithClaimLinksAsync(int id);

    /// <summary>
    /// Rolul cu acest nume, activ sau nu. Comparatia o face baza de date, cu aceleasi
    /// reguli ca indexul unic (fara diferenta intre litere mari si mici).
    /// </summary>
    Task<AppRole?> GetByNameForAdminAsync(string roleName);

    void Add(AppRole role);

    Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId);
}
