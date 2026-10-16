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
        var user = new AppUser { UserId = 1, PasswordHash = "hash-curent" };
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
        var user = new AppUser { UserId = 1, PasswordHash = "hash-curent" };
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-curent", "aceeasi").Returns(true);

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "aceeasi", "aceeasi"), default);

        result.Succeeded.ShouldBeFalse();
        result.ErrorField.ShouldBe("NewPassword");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PeCaleaFericita_SalveazaHashulNou()
    {
        var user = new AppUser { UserId = 1, PasswordHash = "hash-vechi" };
        _users.GetByIdAsync(1).Returns(user);
        _passwords.Verify(user, "hash-vechi", "veche").Returns(true);
        _passwords.Hash(user, "noua").Returns("hash-nou");

        var result = await CreateSut().Handle(new ChangePasswordCommand(1, "veche", "noua"), default);

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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task InlocuiesteLegaturileSiSalveazaOSinguraData()
    {
        var handler = new SetUserDomainsHandler(_users, _unitOfWork);

        await handler.Handle(new SetUserDomainsCommand(1, [10, 20]), default);

        await _users.Received(1).ReplaceUserDomainsAsync(1, Arg.Any<IEnumerable<int>>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void FaraNiciunDomeniu_EsteRespinsDeValidator()
    {
        // Regula "cel putin un domeniu" apartine cazului de utilizare, nu ecranului:
        // trebuie sa fie respectata indiferent de unde vine cererea.
        var result = new SetUserDomainsValidator().Validate(new SetUserDomainsCommand(1, []));

        result.IsValid.ShouldBeFalse();
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
        _users.GetByIdAsync(1).Returns(new AppUser
        {
            UserId = 1,
            Name = "Ana",
            FamilyName = "Popescu",
            Email = "ana@example.com",
            RoleId = 3
        });
        _roles.GetByIdAsync(3).Returns(new AppRole { RoleId = 3, RoleName = "User" });
        _users.GetDomainIdsAsync(1).Returns([10, 20]);
        _domains.GetByIdsAsync(Arg.Any<IEnumerable<int>>()).Returns(
        [
            new TrainingDomain { DomainId = 10, DomainName = "Matematica" },
            new TrainingDomain { DomainId = 20, DomainName = "Limbi straine" }
        ]);

        var profile = await CreateSut().Handle(new GetUserProfileQuery(1), default);

        profile.ShouldNotBeNull();
        profile.FullName.ShouldBe("Ana Popescu");
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
