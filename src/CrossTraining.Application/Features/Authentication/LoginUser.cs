using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.DTOs;
using FluentValidation;

namespace CrossTraining.Application.Features.Authentication;

public sealed record LoginUserQuery(string Email, string Password) : IQuery<AuthResult>;

public sealed class LoginUserValidator : AbstractValidator<LoginUserQuery>
{
    public LoginUserValidator()
    {
        RuleFor(q => q.Email).NotEmpty();
        RuleFor(q => q.Password).NotEmpty();
    }
}

public sealed class LoginUserHandler : IQueryHandler<LoginUserQuery, AuthResult>
{
    private readonly IAppUserRepository _users;
    private readonly IAppRoleRepository _roles;
    private readonly IPasswordService _passwords;

    public LoginUserHandler(IAppUserRepository users, IAppRoleRepository roles, IPasswordService passwords)
    {
        _users = users;
        _roles = roles;
        _passwords = passwords;
    }

    public async Task<AuthResult> Handle(LoginUserQuery query, CancellationToken cancellationToken)
    {
        // Filtrul global de soft-delete face ca un cont dezactivat sa nu fie gasit.
        var user = await _users.GetByEmailAsync(query.Email);

        // Acelasi mesaj pentru email inexistent, cont dezactivat sau parola gresita.
        if (user == null || !_passwords.Verify(user, user.PasswordHash, query.Password))
            return AuthResult.Fail("Invalid email or password!");

        var (roleName, permissions) = await LoadRoleAndPermissionsAsync(user.RoleId);

        var authenticated = new AuthenticatedUserDto(
            user.UserId,
            $"{user.Name} {user.FamilyName}",
            user.Email);

        return AuthResult.Success(authenticated, roleName, permissions);
    }

    private async Task<(string? RoleName, IReadOnlyList<string> Permissions)> LoadRoleAndPermissionsAsync(int? roleId)
    {
        if (roleId == null)
            return (null, []);

        var role = await _roles.GetByIdAsync(roleId.Value);

        // Drepturile dezactivate sunt deja excluse de filtrul global.
        var claims = await _roles.GetClaimsForRoleAsync(roleId.Value);

        return (role?.RoleName, claims.Select(c => c.ClaimName).ToList());
    }
}
