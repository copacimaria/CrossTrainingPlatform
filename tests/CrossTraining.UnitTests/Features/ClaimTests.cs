using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Authorization;
using CrossTraining.Application.Features.Claims;
using CrossTraining.Domain.Entities;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

/// <summary>
/// Drepturile verificate de cod nu pot fi stricate din ecranul de administrare, iar
/// numele raman unice. Fiecare refuz trebuie sa lase baza neatinsa.
/// </summary>
public class ClaimCommandTests
{
    private readonly IAppClaimRepository _claims = Substitute.For<IAppClaimRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>Un drept existent, gasit dupa id si - ca in SQL Server - dupa nume, fara diferenta de litere.</summary>
    private AppClaim Existing(int id, string name, bool isActive = true)
    {
        var claim = new AppClaim { ClaimId = id, ClaimName = name, IsActive = isActive };
        _claims.GetForAdminAsync(id).Returns(claim);
        _claims.GetByNameForAdminAsync(Arg.Is<string>(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
               .Returns(claim);
        return claim;
    }

    private Task<int> NothingWasSaved() => _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    private Task<int> SavedOnce() => _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    // ------------------- REDENUMIRE -------------------

    [Fact]
    public async Task Rename_DreptDeSistem_EsteRefuzat()
    {
        var claim = Existing(1, Permissions.Roles.Manage);

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(1, "altceva"), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("nu poate fi redenumit");
        claim.ClaimName.ShouldBe(Permissions.Roles.Manage);
        await NothingWasSaved();
    }

    [Theory]
    [InlineData("roles.manage")]
    [InlineData("Roles.Manage")]
    [InlineData("  claims.manage  ")]
    public async Task Rename_InNumeleUnuiDreptDeSistem_EsteRefuzat(string newName)
    {
        // Escaladarea pe care o opreste regula: un drept deja acordat rolului tau,
        // redenumit in roles.manage, ti-ar da administrarea tuturor drepturilor.
        var claim = Existing(5, "rapoarte.view");

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(5, newName), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("rezervat");
        claim.ClaimName.ShouldBe("rapoarte.view");
        await NothingWasSaved();
    }

    [Fact]
    public async Task Rename_FaraNicioSchimbare_ReuseseFaraSaSalveze()
    {
        // Salvarea modalului de editare fara modificari nu e o redenumire - nici pentru
        // un drept de sistem.
        Existing(1, Permissions.Claims.View);

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(1, Permissions.Claims.View), default);

        result.Succeeded.ShouldBeTrue();
        await NothingWasSaved();
    }

    [Fact]
    public async Task Rename_InNumeleAltuiDrept_EsteRefuzat()
    {
        var claim = Existing(5, "a");
        Existing(6, "b");

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(5, "B"), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("Exista deja");
        claim.ClaimName.ShouldBe("a");
        await NothingWasSaved();
    }

    [Fact]
    public async Task Rename_InNumeleUnuiDreptDezactivat_SugereazaReactivarea()
    {
        Existing(5, "a");
        Existing(6, "b", isActive: false);

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(5, "b"), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("Reactivati");
    }

    [Fact]
    public async Task Rename_DoarAltaScriereALiterelor_EstePermisa()
    {
        // Baza de date gaseste acelasi drept dupa noul nume; nu e un conflict cu altul.
        var claim = Existing(5, "rapoarte");

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(5, "Rapoarte"), default);

        result.Succeeded.ShouldBeTrue();
        claim.ClaimName.ShouldBe("Rapoarte");
        await SavedOnce();
    }

    [Fact]
    public async Task Rename_PeCaleaFericita_SalveazaNumeleFaraSpatii()
    {
        var claim = Existing(5, "a");

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(5, "  nou  "), default);

        result.Succeeded.ShouldBeTrue();
        claim.ClaimName.ShouldBe("nou");
        await SavedOnce();
    }

    [Fact]
    public async Task Rename_DreptInexistent_EsteRefuzat()
    {
        _claims.GetForAdminAsync(99).Returns((AppClaim?)null);

        var result = await new RenameClaimHandler(_claims, _unitOfWork)
            .Handle(new RenameClaimCommand(99, "x"), default);

        result.Succeeded.ShouldBeFalse();
        await NothingWasSaved();
    }

    // ------------------- CREARE -------------------

    [Fact]
    public async Task Create_CuNumeleUnuiDreptDeSistem_EsteRefuzat()
    {
        var result = await new CreateClaimHandler(_claims, _unitOfWork)
            .Handle(new CreateClaimCommand("Roles.Manage"), default);

        result.Succeeded.ShouldBeFalse();
        _claims.DidNotReceive().Add(Arg.Any<AppClaim>());
        await NothingWasSaved();
    }

    [Fact]
    public async Task Create_CuNumeExistent_EsteRefuzat()
    {
        Existing(6, "rapoarte");

        var result = await new CreateClaimHandler(_claims, _unitOfWork)
            .Handle(new CreateClaimCommand("RAPOARTE"), default);

        result.Succeeded.ShouldBeFalse();
        _claims.DidNotReceive().Add(Arg.Any<AppClaim>());
        await NothingWasSaved();
    }

    [Fact]
    public async Task Create_PeCaleaFericita_AdaugaUnDreptActiv()
    {
        var result = await new CreateClaimHandler(_claims, _unitOfWork)
            .Handle(new CreateClaimCommand(" rapoarte "), default);

        result.Succeeded.ShouldBeTrue();
        _claims.Received(1).Add(Arg.Is<AppClaim>(c => c.ClaimName == "rapoarte" && c.IsActive));
        await SavedOnce();
    }

    // ------------------- DEZACTIVARE / REACTIVARE -------------------

    [Fact]
    public async Task Deactivate_DreptDeSistem_EsteRefuzat()
    {
        // Dezactivat, roles.manage ar disparea din toate rolurile - inclusiv din Admin.
        var claim = Existing(1, Permissions.Roles.Manage);

        var result = await new DeactivateClaimHandler(_claims, _unitOfWork)
            .Handle(new DeactivateClaimCommand(1), default);

        result.Succeeded.ShouldBeFalse();
        claim.IsActive.ShouldBeTrue();
        await NothingWasSaved();
    }

    [Fact]
    public async Task Deactivate_DreptObisnuit_IlDezactiveaza()
    {
        var claim = Existing(5, "rapoarte");

        var result = await new DeactivateClaimHandler(_claims, _unitOfWork)
            .Handle(new DeactivateClaimCommand(5), default);

        result.Succeeded.ShouldBeTrue();
        claim.IsActive.ShouldBeFalse();
        await SavedOnce();
    }

    [Fact]
    public async Task Reactivate_DreptDezactivat_IlReactiveaza()
    {
        var claim = Existing(5, "rapoarte", isActive: false);

        var result = await new ReactivateClaimHandler(_claims, _unitOfWork)
            .Handle(new ReactivateClaimCommand(5), default);

        result.Succeeded.ShouldBeTrue();
        claim.IsActive.ShouldBeTrue();
        await SavedOnce();
    }

    [Fact]
    public async Task Reactivate_DreptDejaActiv_ReuseseFaraSaSalveze()
    {
        Existing(5, "rapoarte");

        var result = await new ReactivateClaimHandler(_claims, _unitOfWork)
            .Handle(new ReactivateClaimCommand(5), default);

        result.Succeeded.ShouldBeTrue();
        await NothingWasSaved();
    }

    [Fact]
    public async Task Reactivate_DreptInexistent_EsteRefuzat()
    {
        _claims.GetForAdminAsync(99).Returns((AppClaim?)null);

        var result = await new ReactivateClaimHandler(_claims, _unitOfWork)
            .Handle(new ReactivateClaimCommand(99), default);

        result.Succeeded.ShouldBeFalse();
    }

    // ------------------- LISTA DE DREPTURI DE SISTEM -------------------

    [Fact]
    public void DrepturileDinCod_SuntToateDeSistem()
    {
        Permissions.All.ShouldAllBe(p => Permissions.IsSystemPermission(p));
        Permissions.IsSystemPermission("ROLES.MANAGE").ShouldBeTrue();
        Permissions.IsSystemPermission("rapoarte.view").ShouldBeFalse();
    }
}
