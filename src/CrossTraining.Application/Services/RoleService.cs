using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class RoleService : IRoleService
{
    private readonly IAppRoleRepository _roles;

    public RoleService(IAppRoleRepository roles)
    {
        _roles = roles;
    }

    public Task<List<AppRole>> GetAllAsync() => _roles.GetAllAsync();
    public Task<AppRole?> GetByIdAsync(int id) => _roles.GetByIdAsync(id);
    public Task<int> CreateAsync(string roleName) => _roles.CreateAsync(roleName);
    public Task UpdateNameAsync(int id, string roleName) => _roles.UpdateNameAsync(id, roleName);
    public Task SoftDeleteAsync(int id) => _roles.SoftDeleteAsync(id);

    public Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId) => _roles.GetClaimsForRoleAsync(roleId);
    public Task<List<int>> GetClaimIdsForRoleAsync(int roleId) => _roles.GetClaimIdsForRoleAsync(roleId);
    public Task AddClaimAsync(int roleId, int claimId) => _roles.AddClaimAsync(roleId, claimId);
    public Task RemoveClaimAsync(int roleId, int claimId) => _roles.RemoveClaimAsync(roleId, claimId);
}
