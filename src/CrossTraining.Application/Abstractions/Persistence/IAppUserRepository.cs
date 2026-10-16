using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IAppUserRepository
{
    /// <summary>Utilizatorul activ cu acest id. Urmarit de context, deci poate fi modificat.</summary>
    Task<AppUser?> GetByIdAsync(int id);

    /// <summary>
    /// Utilizatorul activ, impreuna cu toate legaturile lui catre domenii - inclusiv cele
    /// catre domenii dezactivate, ca <see cref="AppUser.ChooseDomains"/> sa le poata inlocui
    /// pe toate, fara sa lase randuri orfane.
    /// </summary>
    Task<AppUser?> GetWithDomainLinksAsync(int id);

    /// <summary>Utilizatorul activ cu acest email. Conturile dezactivate nu se pot autentifica.</summary>
    Task<AppUser?> GetByEmailAsync(Email email);

    /// <summary>
    /// Exista deja un cont cu acest email, activ sau nu?
    /// Verificarea de la inregistrare trebuie sa vada si conturile dezactivate, altfel
    /// emailul pare liber si inserarea pica pe indexul unic.
    /// </summary>
    Task<bool> EmailExistsAsync(Email email);

    void Add(AppUser user);

    Task<List<int>> GetDomainIdsAsync(int userId);
}
