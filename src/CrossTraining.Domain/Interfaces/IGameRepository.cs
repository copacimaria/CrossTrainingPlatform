using CrossTraining.Domain.Entities;

namespace CrossTraining.Domain.Interfaces;

public interface IGameRepository
{
    Task<List<Game>> GetAllAsync();
    Task<List<Game>> GetByDomainAsync(int domainId);
    Task<Game?> GetByIdAsync(int id);
    Task<int> CreateAsync(Game game);
    Task UpdateAsync(Game game);
    Task SoftDeleteAsync(int id);
}
