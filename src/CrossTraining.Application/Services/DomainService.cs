using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class DomainService : IDomainService
{
    private readonly IDomainRepository _domains;

    public DomainService(IDomainRepository domains)
    {
        _domains = domains;
    }

    public Task<List<TrainingDomain>> GetAllAsync() => _domains.GetAllAsync();
    public Task<List<TrainingDomain>> GetActiveAsync() => _domains.GetActiveAsync();
    public Task<TrainingDomain?> GetByIdAsync(int id) => _domains.GetByIdAsync(id);
    public Task<int> CreateAsync(TrainingDomain domain) => _domains.CreateAsync(domain);

    public Task UpdateAsync(int id, TrainingDomain domain)
    {
        domain.DomainId = id;
        return _domains.UpdateAsync(domain);
    }

    public Task DeleteAsync(int id) => _domains.DeleteAsync(id);
}
