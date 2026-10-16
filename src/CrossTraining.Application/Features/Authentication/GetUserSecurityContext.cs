using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;

namespace CrossTraining.Application.Features.Authentication;

/// <summary>
/// Reincarca rolul si permisiunile unui utilizator deja autentificat.
///
/// Cookie-ul poarta permisiunile de la momentul login-ului. Fara reimprospatare, o
/// permisiune revocata ar ramane valabila pana la expirarea cookie-ului.
/// </summary>
public sealed record GetUserSecurityContextQuery(int UserId) : IQuery<UserSecurityContextDto?>;

public sealed class GetUserSecurityContextHandler
    : IQueryHandler<GetUserSecurityContextQuery, UserSecurityContextDto?>
{
    private readonly IAppUserRepository _users;
    private readonly IAppRoleRepository _roles;

    public GetUserSecurityContextHandler(IAppUserRepository users, IAppRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<UserSecurityContextDto?> Handle(
        GetUserSecurityContextQuery query,
        CancellationToken cancellationToken)
    {
        // Null inseamna "contul nu mai exista sau a fost dezactivat" - apelantul
        // deconecteaza sesiunea.
        var user = await _users.GetByIdAsync(query.UserId);
        if (user == null)
            return null;

        string? roleName = null;
        IReadOnlyList<string> permissions = [];

        if (user.RoleId != null)
        {
            var role = await _roles.GetByIdAsync(user.RoleId.Value);
            roleName = role?.RoleName;

            var claims = await _roles.GetClaimsForRoleAsync(user.RoleId.Value);
            permissions = claims.Select(c => c.ClaimName).ToList();
        }

        return new UserSecurityContextDto(
            user.UserId,
            $"{user.Name} {user.FamilyName}",
            user.Email,
            roleName,
            permissions);
    }
}
