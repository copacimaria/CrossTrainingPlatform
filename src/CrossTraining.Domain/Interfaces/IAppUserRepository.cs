using CrossTraining.Domain.Entities;

namespace CrossTraining.Domain.Interfaces;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(int id);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<int> CreateAsync(AppUser user);
    Task UpdatePasswordHashAsync(int userId, string passwordHash);

    /// <summary>Inlocuieste complet lista de domenii a unui utilizator.</summary>
    Task SetUserDomainsAsync(int userId, IEnumerable<int> domainIds);

    /// <summary>Id-urile domeniilor asociate unui utilizator.</summary>
    Task<List<int>> GetDomainIdsAsync(int userId);
}
