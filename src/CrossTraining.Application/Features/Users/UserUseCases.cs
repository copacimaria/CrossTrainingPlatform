using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Features.Users;

// ------------------- CITIRI -------------------

public sealed record GetUserProfileQuery(int UserId) : IQuery<UserProfileDto?>;

public sealed class GetUserProfileHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IAppUserRepository _users;
    private readonly IAppRoleRepository _roles;
    private readonly IDomainRepository _domains;

    public GetUserProfileHandler(
        IAppUserRepository users,
        IAppRoleRepository roles,
        IDomainRepository domains)
    {
        _users = users;
        _roles = roles;
        _domains = domains;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(query.UserId);
        if (user == null)
            return null;

        string roleName = string.Empty;
        if (user.RoleId != null)
        {
            var role = await _roles.GetByIdAsync(user.RoleId.Value);
            roleName = role?.RoleName ?? string.Empty;
        }

        var domainIds = await _users.GetDomainIdsAsync(query.UserId);
        var domains = await _domains.GetByIdsAsync(domainIds);

        return new UserProfileDto(
            user.FullName,
            user.Email.Value,
            roleName,
            string.Join(", ", domains.Select(d => d.DomainName)));
    }
}

public sealed record GetUserDomainIdsQuery(int UserId) : IQuery<List<int>>;

public sealed class GetUserDomainIdsHandler : IQueryHandler<GetUserDomainIdsQuery, List<int>>
{
    private readonly IAppUserRepository _users;

    public GetUserDomainIdsHandler(IAppUserRepository users) => _users = users;

    public Task<List<int>> Handle(GetUserDomainIdsQuery query, CancellationToken cancellationToken)
        => _users.GetDomainIdsAsync(query.UserId);
}

// ------------------- COMENZI -------------------

public sealed record ChangePasswordCommand(int UserId, string OldPassword, string NewPassword)
    : ICommand<OperationResult>;

public sealed class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand, OperationResult>
{
    private readonly IAppUserRepository _users;
    private readonly IPasswordService _passwords;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordHandler(
        IAppUserRepository users,
        IPasswordService passwords,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwords = passwords;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId);
        if (user == null)
            return OperationResult.Fail("Utilizatorul nu exista.");

        // Verificarea parolei vechi cere hash-ul calculat de infrastructura, deci sta aici.
        if (!_passwords.Verify(user, user.PasswordHash, command.OldPassword))
            return OperationResult.Fail("Parola veche este incorecta!", nameof(command.OldPassword));

        var policy = AppUser.ValidatePassword(command.NewPassword);
        if (!policy.Succeeded)
            return OperationResult.Fail(policy.Error!, nameof(command.NewPassword));

        if (command.NewPassword == command.OldPassword)
            return OperationResult.Fail("Parola noua nu poate fi identica cu cea veche!", nameof(command.NewPassword));

        user.SetPasswordHash(_passwords.Hash(user, command.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}

public sealed record SetUserDomainsCommand(int UserId, IReadOnlyList<int> DomainIds) : ICommand<OperationResult>;

public sealed class SetUserDomainsHandler : ICommandHandler<SetUserDomainsCommand, OperationResult>
{
    private readonly IAppUserRepository _users;
    private readonly IDomainRepository _domains;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserDomainsHandler(IAppUserRepository users, IDomainRepository domains, IUnitOfWork unitOfWork)
    {
        _users = users;
        _domains = domains;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(SetUserDomainsCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetWithDomainLinksAsync(command.UserId);
        if (user == null)
            return OperationResult.Fail("Utilizatorul nu exista.");

        // Doar domenii active si existente: un id gresit, trimis direct, nu trebuie sa ajunga
        // la cheia straina si sa iasa ca eroare 500.
        var requested = command.DomainIds.Distinct().ToList();
        var active = await _domains.GetByIdsAsync(requested);
        if (active.Count != requested.Count)
            return OperationResult.Fail("Unul dintre domeniile alese nu exista sau este dezactivat.");

        var chosen = user.ChooseDomains(requested);
        if (!chosen.Succeeded)
            return OperationResult.From(chosen);

        // Legaturile vechi si cele noi ajung in aceeasi salvare: utilizatorul nu poate ramane
        // fara niciun domeniu la jumatatea operatiei.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
