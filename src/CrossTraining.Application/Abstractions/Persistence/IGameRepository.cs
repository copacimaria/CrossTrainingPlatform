using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IGameRepository
{
    /// <summary>
    /// Jocurile active cu domeniul deja incarcat, intr-o singura interogare.
    /// Inainte se faceau trei interogari si un join in memorie.
    /// </summary>
    Task<List<Game>> GetActiveWithDomainAsync();

    /// <summary>Jocul cu domeniul incarcat, inclusiv daca este dezactivat.</summary>
    Task<Game?> GetForAdminAsync(int id);

    void Add(Game game);
}
