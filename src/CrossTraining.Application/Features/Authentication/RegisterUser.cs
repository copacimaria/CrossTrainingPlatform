using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.Application.Features.Authentication;

public sealed record RegisterUserCommand(string Name, string FamilyName, string Email, string Password)
    : ICommand<AuthResult>;

public sealed class RegisterUserHandler : ICommandHandler<RegisterUserCommand, AuthResult>
{
    /// <summary>Rolul atribuit automat conturilor noi (dbo.AppRole, RoleId = 1: "User").</summary>
    private const int DefaultRoleId = 1;

    private readonly IAppUserRepository _users;
    private readonly IPasswordService _passwords;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public RegisterUserHandler(
        IAppUserRepository users,
        IPasswordService passwords,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _users = users;
        _passwords = passwords;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AuthResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (!email.Succeeded)
            return AuthResult.Fail(email.Error!);

        var policy = AppUser.ValidatePassword(command.Password);
        if (!policy.Succeeded)
            return AuthResult.Fail(policy.Error!);

        // Verificarea vede si conturile dezactivate: emailul are index unic, deci un cont
        // dezactivat blocheaza in continuare adresa. Este o regula care cere acces la date,
        // deci sta aici, nu in entitate.
        if (await _users.EmailExistsAsync(email.Value))
            return AuthResult.Fail("Exista deja un cont cu acest email.");

        var registered = AppUser.Register(
            command.Name,
            command.FamilyName,
            email.Value,
            DefaultRoleId,
            _clock.GetUtcNow().UtcDateTime);

        if (!registered.Succeeded)
            return AuthResult.Fail(registered.Error!);

        var user = registered.Value;

        // Parola se stocheaza doar ca hash, niciodata in text clar.
        user.SetPasswordHash(_passwords.Hash(user, command.Password));

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult.Registered(user.UserId);
    }
}
