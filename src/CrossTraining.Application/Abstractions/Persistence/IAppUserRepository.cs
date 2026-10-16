using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IAppUserRepository
{
    /// <summary>Utilizatorul activ cu acest id. Urmarit de context, deci poate fi modificat.</summary>
    Task<AppUser?> GetByIdAsync(int id);

    /// <summary>Utilizatorul activ cu acest email. Conturile dezactivate nu se pot autentifica.</summary>
    Task<AppUser?> GetByEmailAsync(string email);

    /// <summary>
    /// Exista deja un cont cu acest email, activ sau nu?
    /// Verificarea de la inregistrare trebuie sa vada si conturile dezactivate, altfel
    /// emailul pare liber si inserarea pica pe indexul unic.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    void Add(AppUser user);

    Task<List<int>> GetDomainIdsAsync(int userId);

    /// <summary>Pregateste inlocuirea completa a listei de domenii. Nu salveaza.</summary>
    Task ReplaceUserDomainsAsync(int userId, IEnumerable<int> domainIds);
}
