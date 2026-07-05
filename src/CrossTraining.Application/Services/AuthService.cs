using CrossTraining.Application.DTOs;
using CrossTraining.Application.Interfaces;
using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;

namespace CrossTraining.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAppUserRepository _users;
    private readonly IAppRoleRepository _roles;
    private readonly IPasswordService _passwords;

    public AuthService(IAppUserRepository users, IAppRoleRepository roles, IPasswordService passwords)
    {
        _users = users;
        _roles = roles;
        _passwords = passwords;
    }

    public async Task<AuthResult> RegisterAsync(RegisterViewModel model)
    {
        var existing = await _users.GetByEmailAsync(model.Email);
        if (existing != null)
            return AuthResult.Fail("Exista deja un cont cu acest email.");

        var newUser = new AppUser
        {
            Name = model.Name,
            FamilyName = model.FamilyName,
            Email = model.Email,
            CreationDate = DateTime.Now,
            IsActive = true,
            RoleId = 1
        };

        // Parola se stocheaza doar ca hash, niciodata in text clar.
        newUser.PasswordHash = _passwords.Hash(newUser, model.Password);

        int newUserId = await _users.CreateAsync(newUser);
        return AuthResult.Registered(newUserId);
    }

    public async Task<AuthResult> LoginAsync(LoginViewModel model)
    {
        var user = await _users.GetByEmailAsync(model.Email);

        // Acelasi mesaj pentru email inexistent sau parola gresita.
        if (user == null || !_passwords.Verify(user, user.PasswordHash, model.Password))
            return AuthResult.Fail("Invalid email or password!");

        // Incarcam rolul si permisiunile (claim-urile) pentru a le pune in cookie.
        string? roleName = null;
        var permissions = new List<string>();

        if (user.RoleId != null)
        {
            var role = await _roles.GetByIdAsync(user.RoleId.Value);
            roleName = role?.RoleName;

            var claims = await _roles.GetClaimsForRoleAsync(user.RoleId.Value);
            permissions = claims.Where(c => c.IsActive).Select(c => c.ClaimName).ToList();
        }

        return AuthResult.Success(user, roleName, permissions);
    }
}
