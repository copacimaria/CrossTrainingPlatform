using CrossTraining.Domain.Entities;

namespace CrossTraining.Domain.Interfaces;

public interface IDomainRepository
{
    Task<List<TrainingDomain>> GetAllAsync();
    Task<List<TrainingDomain>> GetActiveAsync();
    Task<TrainingDomain?> GetByIdAsync(int id);
    Task<List<TrainingDomain>> GetByIdsAsync(IEnumerable<int> ids);
    Task<int> CreateAsync(TrainingDomain domain);
    Task UpdateAsync(TrainingDomain domain);
    Task DeleteAsync(int id);
}
