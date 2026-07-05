using CrossTraining.Application.DTOs;
using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Interfaces;

/// <summary>Logica legata de profilul utilizatorului (parola, domenii, date afisate).</summary>
public interface IUserService
{
    Task<AppUser?> GetByIdAsync(int userId);
    Task<ProfileViewModel?> GetProfileAsync(int userId);

    Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model);

    Task<List<int>> GetUserDomainIdsAsync(int userId);
    Task SetUserDomainsAsync(int userId, IEnumerable<int> domainIds);
}
