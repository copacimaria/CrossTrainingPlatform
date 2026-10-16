using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Persistence;

public interface IDomainRepository
{
    /// <summary>Toate domeniile, inclusiv cele dezactivate (ecran de administrare).</summary>
    Task<List<TrainingDomain>> GetAllForAdminAsync();

    Task<List<TrainingDomain>> GetActiveAsync();

    /// <summary>Domeniul, inclusiv daca este dezactivat. Necesar ca sa poata fi reactivat.</summary>
    Task<TrainingDomain?> GetForAdminAsync(int id);

    Task<List<TrainingDomain>> GetByIdsAsync(IEnumerable<int> ids);

    void Add(TrainingDomain domain);
}
