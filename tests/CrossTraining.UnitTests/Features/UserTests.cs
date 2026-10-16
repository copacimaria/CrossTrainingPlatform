using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Application.Features.Users;
using CrossTraining.Domain.Entities;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

public class ChangePasswordTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangePasswordHandler CreateSut() => new(_users, _passwords, _unitOfWork);

    [Fact]
    public async Task CuParolaVecheGresita_Esueaza()
    {
        var user = Loaded.User(1, passwordHash: "hash-curent");
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-curent", "gresita").Returns(false);

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "gresita", "parola-noua"), default);

        result.Succeeded.ShouldBeFalse();
        result.ErrorField.ShouldBe("OldPassword");
        user.PasswordHash.ShouldBe("hash-curent");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CuParolaNouaIdenticaCuCeaVeche_Esueaza()
    {
        var user = Loaded.User(1, passwordHash: "hash-curent");
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-curent", "aceeasi").Returns(true);

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "aceeasi", "aceeasi"), default);

        result.Succeeded.ShouldBeFalse();
        result.ErrorField.ShouldBe("NewPassword");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CuParolaNouaPreaScurta_Esueaza()
    {
        // Regula de lungime este a domeniului (AppUser.ValidatePassword), deci se aplica si
        // unei cereri care ocoleste formularul.
        var user = Loaded.User(1, passwordHash: "hash-curent");
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-curent", "veche-buna").Returns(true);

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "veche-buna", "abc"), default);

        result.Succeeded.ShouldBeFalse();
        result.ErrorField.ShouldBe("NewPassword");
        user.PasswordHash.ShouldBe("hash-curent");
    }

    [Fact]
    public async Task PeCaleaFericita_SalveazaHashulNou()
    {
        var user = Loaded.User(1, passwordHash: "hash-vechi");
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-vechi", "veche").Returns(true);
        _passwords.Hash(user, "parola-noua").Returns("hash-nou");

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "veche", "parola-noua"), default);

        result.Succeeded.ShouldBeTrue();
        user.PasswordHash.ShouldBe("hash-nou");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CandUtilizatorulNuExista_Esueaza()
    {
        _users.GetByIdAsync(99).Returns((AppUser?)null);

        var result = await CreateSut().Handle(new ChangePasswordCommand(99, "a", "b"), default);

        result.Succeeded.ShouldBeFalse();
    }
}

public class SetUserDomainsTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IDomainRepository _domains = Substitute.For<IDomainRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SetUserDomainsHandler CreateSut() => new(_users, _domains, _unitOfWork);

    [Fact]
    public async Task InlocuiesteLegaturileSiSalveazaOSinguraData()
    {
        var user = Loaded.User(1);
        _users.GetWithDomainLinksAsync(1).Returns(user);
        _domains.GetByIdsAsync(Arg.Any<IEnumerable<int>>())
                .Returns([Loaded.Domain(10, "Matematica"), Loaded.Domain(20, "Chimie")]);

        var result = await CreateSut().Handle(new SetUserDomainsCommand(1, [10, 20]), default);

        result.Succeeded.ShouldBeTrue();
        user.UserDomains.Select(ud => ud.DomainId).ShouldBe([10, 20], ignoreOrder: true);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FaraNiciunDomeniu_EsteRefuzat()
    {
        // Regula "cel putin un domeniu" apartine entitatii, nu ecranului: trebuie sa fie
        // respectata indiferent de unde vine cererea.
        _users.GetWithDomainLinksAsync(1).Returns(Loaded.User(1));
        _domains.GetByIdsAsync(Arg.Any<IEnumerable<int>>()).Returns([]);

        var result = await CreateSut().Handle(new SetUserDomainsCommand(1, []), default);

        result.Succeeded.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DomeniuInexistentSauDezactivat_EsteRefuzatInLocDeEroareDeCheieStraina()
    {
        var user = Loaded.User(1);
        _users.GetWithDomainLinksAsync(1).Returns(user);
        _domains.GetByIdsAsync(Arg.Any<IEnumerable<int>>()).Returns([Loaded.Domain(10, "Matematica")]);

        var result = await CreateSut().Handle(new SetUserDomainsCommand(1, [10, 999]), default);

        result.Succeeded.ShouldBeFalse();
        user.UserDomains.ShouldBeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class GetUserProfileTests
{
    private readonly IAppUserRepository _users = Substitute.For<IAppUserRepository>();
    private readonly IAppRoleRepository _roles = Substitute.For<IAppRoleRepository>();
    private readonly IDomainRepository _domains = Substitute.For<IDomainRepository>();

    private GetUserProfileHandler CreateSut() => new(_users, _roles, _domains);

    [Fact]
    public async Task CompuneNumeleRoluluiSiDomeniile()
    {
        _users.GetByIdAsync(1).Returns(Loaded.User(1, "ana@example.com", "Ana", "Popescu", roleId: 3));
        _roles.GetByIdAsync(3).Returns(Loaded.Role(3, "User"));
        _users.GetDomainIdsAsync(1).Returns([10, 20]);
        _domains.GetByIdsAsync(Arg.Any<IEnumerable<int>>())
                .Returns([Loaded.Domain(10, "Matematica"), Loaded.Domain(20, "Limbi straine")]);

        var profile = await CreateSut().Handle(new GetUserProfileQuery(1), default);

        profile.ShouldNotBeNull();
        profile.FullName.ShouldBe("Ana Popescu");
        profile.Email.ShouldBe("ana@example.com");
        profile.Role.ShouldBe("User");
        profile.Domains.ShouldBe("Matematica, Limbi straine");
    }

    [Fact]
    public async Task CandUtilizatorulNuExista_IntoarceNull()
    {
        _users.GetByIdAsync(99).Returns((AppUser?)null);

        (await CreateSut().Handle(new GetUserProfileQuery(99), default)).ShouldBeNull();
    }
}
