using CrossTraining.Application.DTOs;
using CrossTraining.Application.Interfaces;
using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class UserService : IUserService
{
    private readonly IAppUserRepository _users;
    private readonly IAppRoleRepository _roles;
    private readonly IDomainRepository _domains;
    private readonly IPasswordService _passwords;

    public UserService(
        IAppUserRepository users,
        IAppRoleRepository roles,
        IDomainRepository domains,
        IPasswordService passwords)
    {
        _users = users;
        _roles = roles;
        _domains = domains;
        _passwords = passwords;
    }

    public Task<AppUser?> GetByIdAsync(int userId) => _users.GetByIdAsync(userId);

    public async Task<ProfileViewModel?> GetProfileAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return null;

        string roleName = string.Empty;
        if (user.RoleId != null)
        {
            var role = await _roles.GetByIdAsync(user.RoleId.Value);
            roleName = role?.RoleName ?? string.Empty;
        }

        var domainIds = await _users.GetDomainIdsAsync(userId);
        var domains = await _domains.GetByIdsAsync(domainIds);
        string domainNames = string.Join(", ", domains.Select(d => d.DomainName));

        return new ProfileViewModel
        {
            FullName = $"{user.Name} {user.FamilyName}",
            Email = user.Email,
            Role = roleName,
            Domains = domainNames
        };
    }

    public async Task<OperationResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        if (!_passwords.Verify(user, user.PasswordHash, model.OldPassword))
            return OperationResult.Fail("Parola veche este incorecta!", nameof(model.OldPassword));

        if (model.NewPassword == model.OldPassword)
            return OperationResult.Fail("Parola noua nu poate fi identica cu cea veche!", nameof(model.NewPassword));

        var newHash = _passwords.Hash(user, model.NewPassword);
        await _users.UpdatePasswordHashAsync(userId, newHash);

        return OperationResult.Success();
    }

    public Task<List<int>> GetUserDomainIdsAsync(int userId) => _users.GetDomainIdsAsync(userId);

    public Task SetUserDomainsAsync(int userId, IEnumerable<int> domainIds)
        => _users.SetUserDomainsAsync(userId, domainIds);
}
