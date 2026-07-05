using CrossTraining.Domain.Entities;

namespace CrossTraining.Domain.Interfaces;

public interface IAppClaimRepository
{
    Task<List<AppClaim>> GetAllAsync();
    Task<AppClaim?> GetByIdAsync(int id);
    Task<int> CreateAsync(string claimName);
    Task UpdateNameAsync(int id, string claimName);
    Task DeleteAsync(int id);
}
