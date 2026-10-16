using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.Features.Authentication;
using CrossTraining.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

public class RegisterUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);

    private RegisterUserHandler CreateSut() => new(_users, _passwords, _unitOfWork, _clock);

    private static RegisterUserCommand Command(string email, string password = "parola123")
        => new("Ana", "Popescu", email, password);

    [Fact]
    public async Task CandEmailulExista_Esueaza()
    {
        _users.EmailExistsAsync("ocupat@example.com").Returns(true);

        var result = await CreateSut().Handle(Command("ocupat@example.com"), default);

        result.Succeeded.ShouldBeFalse();
        _users.DidNotReceive().Add(Arg.Any<AppUser>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerificaSiConturileDezactivate()
    {
        // Emailul are index unic, deci un cont dezactivat blocheaza in continuare adresa.
        // Daca verificarea ar folosi interogarea filtrata, adresa ar parea libera si
        // inserarea ar pica pe index.
        await CreateSut().Handle(Command("cineva@example.com"), default);

        await _users.Received(1).EmailExistsAsync("cineva@example.com");
    }

    [Fact]
    public async Task StocheazaDoarHashul_NiciodataParolaInClar()
    {
        _users.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        _passwords.Hash(Arg.Any<AppUser>(), "parola-mea").Returns("$argon2id$hash");

        var result = await CreateSut().Handle(Command("nou@example.com", "parola-mea"), default);

        result.Succeeded.ShouldBeTrue();

        var saved = CaptureAddedUser();
        saved.PasswordHash.ShouldBe("$argon2id$hash");
        saved.PasswordHash.ShouldNotContain("parola-mea");
    }

    [Fact]
    public async Task FoloseresteCeasulInjectat_NuOraSistemului()
    {
        await CreateSut().Handle(Command("nou@example.com"), default);

        CaptureAddedUser().CreatedAt.ShouldBe(Now.UtcDateTime);
    }

    private AppUser CaptureAddedUser()
        => (AppUser)_users.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == nameof(IAppUserRepository.Add))
            .GetArguments()[0]!;
}

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Theory]
    [InlineData("", "Popescu", "a@b.co", "parola123")]
    [InlineData("Ana", "", "a@b.co", "parola123")]
    [InlineData("Ana", "Popescu", "nu-e-email", "parola123")]
    [InlineData("Ana", "Popescu", "a@b.co", "scurt")]
    public void CereriInvalide_SuntRespinse(string name, string familyName, string email, string password)
    {
        var result = _validator.Validate(new RegisterUserCommand(name, familyName, email, password));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CerereValida_TreceDeValidare()
    {
        var result = _validator.Validate(new RegisterUserCommand("Ana", "Popescu", "a@b.co", "parola123"));

        result.IsValid.ShouldBeTrue();
    }
}

public class LoginUserTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IAppRoleRepository _roles = Substitute.For<IAppRoleRepository>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();

    private LoginUserHandler CreateSut() => new(_users, _roles, _passwords);

    [Fact]
    public async Task EmailInexistentSiParolaGresita_DauAcelasiMesaj()
    {
        _users.GetByEmailAsync("nimeni@example.com").Returns((AppUser?)null);
        var inexistent = await CreateSut().Handle(new LoginUserQuery("nimeni@example.com", "x"), default);

        var user = new AppUser { UserId = 1, Email = "cineva@example.com", PasswordHash = "h" };
        _users.GetByEmailAsync("cineva@example.com").Returns(user);
        _passwords.Verify(user, "h", Arg.Any<string>()).Returns(false);
        var parolaGresita = await CreateSut().Handle(new LoginUserQuery("cineva@example.com", "x"), default);

        // Mesaje identice: altfel se poate afla care adrese au cont.
        inexistent.Succeeded.ShouldBeFalse();
        parolaGresita.Succeeded.ShouldBeFalse();
        inexistent.Error.ShouldBe(parolaGresita.Error);
    }

    [Fact]
    public async Task LoginReusit_IncarcaRolulSiPermisiunile()
    {
        var user = new AppUser { UserId = 5, Email = "admin@example.com", PasswordHash = "h", RoleId = 2 };
        _users.GetByEmailAsync("admin@example.com").Returns(user);
        _passwords.Verify(user, "h", Arg.Any<string>()).Returns(true);
        _roles.GetByIdAsync(2).Returns(new AppRole { RoleId = 2, RoleName = "Admin" });
        _roles.GetClaimsForRoleAsync(2).Returns(
        [
            new AppClaim { ClaimId = 1, ClaimName = "games.manage", IsActive = true }
        ]);

        var result = await CreateSut().Handle(new LoginUserQuery("admin@example.com", "x"), default);

        result.Succeeded.ShouldBeTrue();
        result.RoleName.ShouldBe("Admin");
        result.Permissions.ShouldBe(["games.manage"]);
    }

    [Fact]
    public async Task NuExpuneEntitateaUtilizatorului()
    {
        var user = new AppUser
        {
            UserId = 5,
            Name = "Ana",
            FamilyName = "Popescu",
            Email = "ana@example.com",
            PasswordHash = "$argon2id$secret"
        };
        _users.GetByEmailAsync("ana@example.com").Returns(user);
        _passwords.Verify(user, user.PasswordHash, Arg.Any<string>()).Returns(true);

        var result = await CreateSut().Handle(new LoginUserQuery("ana@example.com", "x"), default);

        result.User.ShouldNotBeNull();
        result.User.UserId.ShouldBe(5);
        result.User.FullName.ShouldBe("Ana Popescu");
        result.User.Email.ShouldBe("ana@example.com");
    }

    [Fact]
    public async Task UtilizatorFaraRol_NuAreNiciPermisiuni()
    {
        var user = new AppUser { UserId = 9, Email = "user@example.com", PasswordHash = "h", RoleId = null };
        _users.GetByEmailAsync("user@example.com").Returns(user);
        _passwords.Verify(user, "h", Arg.Any<string>()).Returns(true);

        var result = await CreateSut().Handle(new LoginUserQuery("user@example.com", "x"), default);

        result.Succeeded.ShouldBeTrue();
        result.RoleName.ShouldBeNull();
        result.Permissions.ShouldBeEmpty();
        await _roles.DidNotReceive().GetClaimsForRoleAsync(Arg.Any<int>());
    }
}

public class GetUserSecurityContextTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IAppRoleRepository _roles = Substitute.For<IAppRoleRepository>();

    private GetUserSecurityContextHandler CreateSut() => new(_users, _roles);

    [Fact]
    public async Task ContDisparutSauDezactivat_IntoarceNull()
    {
        // Interogarea este filtrata de soft-delete, deci un cont dezactivat "nu exista".
        // Apelantul deconecteaza sesiunea pe baza acestui null.
        _users.GetByIdAsync(7).Returns((AppUser?)null);

        (await CreateSut().Handle(new GetUserSecurityContextQuery(7), default)).ShouldBeNull();
    }

    [Fact]
    public async Task ContActiv_IntoarcePermisiunileCurente()
    {
        _users.GetByIdAsync(7).Returns(new AppUser
        {
            UserId = 7,
            Name = "Ana",
            FamilyName = "Popescu",
            Email = "ana@example.com",
            RoleId = 2
        });
        _roles.GetByIdAsync(2).Returns(new AppRole { RoleId = 2, RoleName = "Admin" });
        _roles.GetClaimsForRoleAsync(2).Returns(
        [
            new AppClaim { ClaimId = 1, ClaimName = "roles.manage", IsActive = true }
        ]);

        var context = await CreateSut().Handle(new GetUserSecurityContextQuery(7), default);

        context.ShouldNotBeNull();
        context.FullName.ShouldBe("Ana Popescu");
        context.RoleName.ShouldBe("Admin");
        context.Permissions.ShouldBe(["roles.manage"]);
    }
}
