using CrossTraining.Domain.Entities;

namespace CrossTraining.Domain.Interfaces;

public interface IAppRoleRepository
{
    Task<List<AppRole>> GetAllAsync();
    Task<AppRole?> GetByIdAsync(int id);
    Task<int> CreateAsync(string roleName);
    Task UpdateNameAsync(int id, string roleName);
    Task SoftDeleteAsync(int id);

    Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId);
    Task<List<int>> GetClaimIdsForRoleAsync(int roleId);
    Task AddClaimAsync(int roleId, int claimId);
    Task RemoveClaimAsync(int roleId, int claimId);
}
