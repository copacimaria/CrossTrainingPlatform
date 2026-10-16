using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.DTOs;
using FluentValidation;

namespace CrossTraining.Application.Features.Users;

// ------------------- CITIRI -------------------

public sealed record GetUserProfileQuery(int UserId) : IQuery<UserProfileDto?>;

public sealed class GetUserProfileValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileValidator() => RuleFor(q => q.UserId).GreaterThan(0);
}

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
            $"{user.Name} {user.FamilyName}",
            user.Email,
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

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.UserId).GreaterThan(0);
        RuleFor(c => c.OldPassword).NotEmpty();
        RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(6);
    }
}

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
            return OperationResult.Fail("User not found");

        // Regulile de mai jos cer acces la date sau compara doua campuri intre ele, deci
        // se intorc ca rezultat de business, nu ca eroare de validare.
        if (!_passwords.Verify(user, user.PasswordHash, command.OldPassword))
            return OperationResult.Fail("Parola veche este incorecta!", nameof(command.OldPassword));

        if (command.NewPassword == command.OldPassword)
            return OperationResult.Fail("Parola noua nu poate fi identica cu cea veche!", nameof(command.NewPassword));

        // Entitatea este urmarita de context, deci e suficient sa ii schimbam campul.
        user.PasswordHash = _passwords.Hash(user, command.NewPassword);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}

public sealed record SetUserDomainsCommand(int UserId, IReadOnlyList<int> DomainIds) : ICommand<bool>;

public sealed class SetUserDomainsValidator : AbstractValidator<SetUserDomainsCommand>
{
    public SetUserDomainsValidator()
    {
        RuleFor(c => c.UserId).GreaterThan(0);
        RuleFor(c => c.DomainIds).NotEmpty().WithMessage("Selecteaza cel putin un domeniu.");
        RuleForEach(c => c.DomainIds).GreaterThan(0);
    }
}

public sealed class SetUserDomainsHandler : ICommandHandler<SetUserDomainsCommand, bool>
{
    private readonly IAppUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserDomainsHandler(IAppUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SetUserDomainsCommand command, CancellationToken cancellationToken)
    {
        // Stergerea legaturilor vechi si adaugarea celor noi ajung in aceeasi tranzactie:
        // utilizatorul nu poate ramane fara niciun domeniu la jumatatea operatiei.
        await _users.ReplaceUserDomainsAsync(command.UserId, command.DomainIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
