using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.Features.Authentication;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;
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

    public RegisterUserTests()
        => _passwords.Hash(Arg.Any<AppUser>(), Arg.Any<string>()).Returns("$argon2id$hash");

    private RegisterUserHandler CreateSut() => new(_users, _passwords, _unitOfWork, _clock);

    private static RegisterUserCommand Command(string email, string password = "parola123")
        => new("Ana", "Popescu", email, password);

    [Fact]
    public async Task CandEmailulExista_Esueaza()
    {
        _users.EmailExistsAsync(Email.Create("ocupat@example.com").Value).Returns(true);

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

        await _users.Received(1).EmailExistsAsync(Email.Create("cineva@example.com").Value);
    }

    [Fact]
    public async Task EmailulEsteNormalizatInainteDeVerificareSiSalvare()
    {
        // " Ana@Example.COM " si "ana@example.com" sunt aceeasi adresa: verificarea de
        // duplicat si contul salvat trebuie sa vada aceeasi valoare.
        await CreateSut().Handle(Command("  Ana@Example.COM "), default);

        await _users.Received(1).EmailExistsAsync(Email.Create("ana@example.com").Value);
        CaptureAddedUser().Email.Value.ShouldBe("ana@example.com");
    }

    [Fact]
    public async Task StocheazaDoarHashul_NiciodataParolaInClar()
    {
        _passwords.Hash(Arg.Any<AppUser>(), "parola-mea").Returns("$argon2id$hash-special");

        var result = await CreateSut().Handle(Command("nou@example.com", "parola-mea"), default);

        result.Succeeded.ShouldBeTrue();

        var saved = CaptureAddedUser();
        saved.PasswordHash.ShouldBe("$argon2id$hash-special");
        saved.PasswordHash.ShouldNotContain("parola-mea");
    }

    [Fact]
    public async Task FoloseresteCeasulInjectat_NuOraSistemului()
    {
        await CreateSut().Handle(Command("nou@example.com"), default);

        CaptureAddedUser().CreatedAt.ShouldBe(Now.UtcDateTime);
    }

    [Theory]
    [InlineData("", "Popescu", "a@b.co", "parola123")]
    [InlineData("Ana", "", "a@b.co", "parola123")]
    [InlineData("Ana", "Popescu", "nu-e-email", "parola123")]
    [InlineData("Ana", "Popescu", "a@b.co", "scurt")]
    public async Task CereriInvalide_SuntRefuzateFaraSaSeSalvezeNimic(
        string name,
        string familyName,
        string email,
        string password)
    {
        // Regulile sunt ale domeniului (Email, AppUser), nu ale unui validator separat:
        // trebuie respectate indiferent de unde vine cererea.
        var result = await CreateSut().Handle(new RegisterUserCommand(name, familyName, email, password), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        _users.DidNotReceive().Add(Arg.Any<AppUser>());
    }

    private AppUser CaptureAddedUser()
        => (AppUser)_users.ReceivedCalls()
            .First(c => c.GetMethodInfo().Name == nameof(IAppUserRepository.Add))
            .GetArguments()[0]!;
}

public class LoginUserTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IAppRoleRepository _roles = Substitute.For<IAppRoleRepository>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();

    private LoginUserHandler CreateSut() => new(_users, _roles, _passwords);

    private static Email EmailOf(string value) => Email.Create(value).Value;

    [Fact]
    public async Task EmailInexistentSiParolaGresita_DauAcelasiMesaj()
    {
        _users.GetByEmailAsync(EmailOf("nimeni@example.com")).Returns((AppUser?)null);
        var inexistent = await CreateSut().Handle(new LoginUserQuery("nimeni@example.com", "x"), default);

        var user = Loaded.User(1, "cineva@example.com", passwordHash: "h");
        _users.GetByEmailAsync(EmailOf("cineva@example.com")).Returns(user);
        _passwords.Verify(user, "h", Arg.Any<string>()).Returns(false);
        var parolaGresita = await CreateSut().Handle(new LoginUserQuery("cineva@example.com", "x"), default);

        // Mesaje identice: altfel se poate afla care adrese au cont.
        inexistent.Succeeded.ShouldBeFalse();
        parolaGresita.Succeeded.ShouldBeFalse();
        inexistent.Error.ShouldBe(parolaGresita.Error);
    }

    [Fact]
    public async Task EmailInvalid_DaAcelasiMesajFaraSaAtingaBaza()
    {
        var invalid = await CreateSut().Handle(new LoginUserQuery("nu-e-email", "x"), default);
        var inexistent = await CreateSut().Handle(new LoginUserQuery("nimeni@example.com", "x"), default);

        invalid.Error.ShouldBe(inexistent.Error);
        await _users.Received(1).GetByEmailAsync(Arg.Any<Email>());
    }

    [Fact]
    public async Task LoginReusit_IncarcaRolulSiPermisiunile()
    {
        var user = Loaded.User(5, "admin@example.com", roleId: 2, passwordHash: "h");
        _users.GetByEmailAsync(EmailOf("admin@example.com")).Returns(user);
        _passwords.Verify(user, "h", Arg.Any<string>()).Returns(true);
        _roles.GetByIdAsync(2).Returns(Loaded.Role(2, "Admin"));
        _roles.GetClaimsForRoleAsync(2).Returns([Loaded.Claim(1, "games.manage")]);

        var result = await CreateSut().Handle(new LoginUserQuery("admin@example.com", "x"), default);

        result.Succeeded.ShouldBeTrue();
        result.RoleName.ShouldBe("Admin");
        result.Permissions.ShouldBe(["games.manage"]);
    }

    [Fact]
    public async Task NuExpuneEntitateaUtilizatorului()
    {
        var user = Loaded.User(5, "ana@example.com", "Ana", "Popescu", roleId: null, passwordHash: "$argon2id$secret");
        _users.GetByEmailAsync(EmailOf("ana@example.com")).Returns(user);
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
        var user = Loaded.User(9, "user@example.com", roleId: null, passwordHash: "h");
        _users.GetByEmailAsync(EmailOf("user@example.com")).Returns(user);
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
        _users.GetByIdAsync(7).Returns(Loaded.User(7, "ana@example.com", "Ana", "Popescu", roleId: 2));
        _roles.GetByIdAsync(2).Returns(Loaded.Role(2, "Admin"));
        _roles.GetClaimsForRoleAsync(2).Returns([Loaded.Claim(1, "roles.manage")]);

        var context = await CreateSut().Handle(new GetUserSecurityContextQuery(7), default);

        context.ShouldNotBeNull();
        context.FullName.ShouldBe("Ana Popescu");
        context.RoleName.ShouldBe("Admin");
        context.Permissions.ShouldBe(["roles.manage"]);
    }
}
