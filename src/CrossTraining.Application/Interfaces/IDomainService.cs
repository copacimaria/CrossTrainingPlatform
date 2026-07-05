using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Interfaces;

public interface IDomainService
{
    Task<List<TrainingDomain>> GetAllAsync();
    Task<List<TrainingDomain>> GetActiveAsync();
    Task<TrainingDomain?> GetByIdAsync(int id);
    Task<int> CreateAsync(TrainingDomain domain);
    Task UpdateAsync(int id, TrainingDomain domain);
    Task DeleteAsync(int id);
}
