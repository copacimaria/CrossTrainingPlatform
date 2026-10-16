using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using FluentValidation;

namespace CrossTraining.Application.Features.Authentication;

public sealed record RegisterUserCommand(string Name, string FamilyName, string Email, string Password)
    : ICommand<AuthResult>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.FamilyName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(c => c.Password).NotEmpty().MinimumLength(6);
    }
}

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
        // Verificarea vede si conturile dezactivate: emailul are index unic, deci un cont
        // dezactivat blocheaza in continuare adresa. Este o regula de business, nu o
        // problema de format, deci se intoarce ca rezultat, nu ca exceptie de validare.
        if (await _users.EmailExistsAsync(command.Email))
            return AuthResult.Fail("Exista deja un cont cu acest email.");

        var newUser = new AppUser
        {
            Name = command.Name,
            FamilyName = command.FamilyName,
            Email = command.Email,
            CreatedAt = _clock.GetUtcNow().UtcDateTime,
            IsActive = true,
            RoleId = DefaultRoleId
        };

        // Parola se stocheaza doar ca hash, niciodata in text clar.
        newUser.PasswordHash = _passwords.Hash(newUser, command.Password);

        _users.Add(newUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult.Registered(newUser.UserId);
    }
}
