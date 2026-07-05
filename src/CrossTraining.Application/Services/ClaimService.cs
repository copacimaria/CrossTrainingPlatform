using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class ClaimService : IClaimService
{
    private readonly IAppClaimRepository _claims;

    public ClaimService(IAppClaimRepository claims)
    {
        _claims = claims;
    }

    public Task<List<AppClaim>> GetAllAsync() => _claims.GetAllAsync();
    public Task<AppClaim?> GetByIdAsync(int id) => _claims.GetByIdAsync(id);
    public Task<int> CreateAsync(string claimName) => _claims.CreateAsync(claimName);
    public Task UpdateNameAsync(int id, string claimName) => _claims.UpdateNameAsync(id, claimName);
    public Task DeleteAsync(int id) => _claims.DeleteAsync(id);
}
