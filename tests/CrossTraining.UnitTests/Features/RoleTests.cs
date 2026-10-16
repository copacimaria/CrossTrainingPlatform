using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Authorization;
using CrossTraining.Application.Features.Roles;
using CrossTraining.Domain.Entities;
using NSubstitute;

namespace CrossTraining.UnitTests.Features;

/// <summary>
/// Rolurile User si Admin nu pot fi stricate din ecranul de administrare, iar Admin nu
/// poate pierde drepturile de sistem. Altfel, o singura apasare gresita lasa aplicatia
/// fara administrator, iar refacerea se poate face doar direct in baza de date.
/// </summary>
public class RoleCommandTests
{
    private readonly IAppRoleRepository _roles = Substitute.For<IAppRoleRepository>();
    private readonly IAppClaimRepository _claims = Substitute.For<IAppClaimRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>Un rol existent, gasit dupa id si - ca in SQL Server - dupa nume, fara diferenta de litere.</summary>
    private AppRole ExistingRole(int id, string name, bool isActive = true)
    {
        var role = new AppRole { RoleId = id, RoleName = name, IsActive = isActive };
        _roles.GetForAdminAsync(id).Returns(role);
        _roles.GetByNameForAdminAsync(Arg.Is<string>(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
              .Returns(role);
        return role;
    }

    private AppClaim ExistingClaim(int id, string name, bool isActive = true)
    {
        var claim = new AppClaim { ClaimId = id, ClaimName = name, IsActive = isActive };
        _claims.GetForAdminAsync(id).Returns(claim);
        return claim;
    }

    private RoleClaim ExistingLink(int roleId, int claimId)
    {
        var link = new RoleClaim { RoleId = roleId, ClaimId = claimId };
        _roles.GetRoleClaimAsync(roleId, claimId).Returns(link);
        return link;
    }

    private Task<int> NothingWasSaved() => _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    private Task<int> SavedOnce() => _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    // ------------------- ROLURI DE SISTEM -------------------

    [Theory]
    [InlineData(SystemRoles.Admin)]
    [InlineData(SystemRoles.User)]
    public async Task Deactivate_RolDeSistem_EsteRefuzat(string roleName)
    {
        var role = ExistingRole(1, roleName);

        var result = await new DeactivateRoleHandler(_roles, _unitOfWork)
            .Handle(new DeactivateRoleCommand(1), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("nu poate fi dezactivat");
        role.IsActive.ShouldBeTrue();
        await NothingWasSaved();
    }

    [Theory]
    [InlineData(SystemRoles.Admin)]
    [InlineData(SystemRoles.User)]
    public async Task Rename_RolDeSistem_EsteRefuzat(string roleName)
    {
        var role = ExistingRole(1, roleName);

        var result = await new RenameRoleHandler(_roles, _unitOfWork)
            .Handle(new RenameRoleCommand(1, "Altceva"), default);

        result.Succeeded.ShouldBeFalse();
        role.RoleName.ShouldBe(roleName);
        await NothingWasSaved();
    }

    [Fact]
    public async Task Rename_InNumeleUnuiRolDeSistem_EsteRefuzat()
    {
        var role = ExistingRole(5, "Editori");

        var result = await new RenameRoleHandler(_roles, _unitOfWork)
            .Handle(new RenameRoleCommand(5, "admin"), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("rezervat");
        role.RoleName.ShouldBe("Editori");
        await NothingWasSaved();
    }

    [Fact]
    public async Task Create_CuNumeleUnuiRolDeSistem_EsteRefuzat()
    {
        var result = await new CreateRoleHandler(_roles, _unitOfWork)
            .Handle(new CreateRoleCommand("ADMIN"), default);

        result.Succeeded.ShouldBeFalse();
        _roles.DidNotReceive().Add(Arg.Any<AppRole>());
        await NothingWasSaved();
    }

    // ------------------- NUME UNICE -------------------

    [Fact]
    public async Task Create_CuNumeleUnuiRolDezactivat_SugereazaReactivarea()
    {
        ExistingRole(7, "Editori", isActive: false);

        var result = await new CreateRoleHandler(_roles, _unitOfWork)
            .Handle(new CreateRoleCommand("editori"), default);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldNotBeNull().ShouldContain("Reactivati");
        _roles.DidNotReceive().Add(Arg.Any<AppRole>());
    }

    [Fact]
    public async Task Create_PeCaleaFericita_AdaugaUnRolActiv()
    {
        var result = await new CreateRoleHandler(_roles, _unitOfWork)
            .Handle(new CreateRoleCommand(" Editori "), default);

        result.Succeeded.ShouldBeTrue();
        _roles.Received(1).Add(Arg.Is<AppRole>(r => r.RoleName == "Editori" && r.IsActive));
        await SavedOnce();
    }

    [Fact]
    public async Task Rename_InNumeleAltuiRol_EsteRefuzat()
    {
        var role = ExistingRole(5, "Editori");
        ExistingRole(6, "Profesori");

        var result = await new RenameRoleHandler(_roles, _unitOfWork)
            .Handle(new RenameRoleCommand(5, "profesori"), default);

        result.Succeeded.ShouldBeFalse();
        role.RoleName.ShouldBe("Editori");
        await NothingWasSaved();
    }

    // ------------------- DEZACTIVARE / REACTIVARE -------------------

    [Fact]
    public async Task Deactivate_RolObisnuit_IlDezactiveaza()
    {
        var role = ExistingRole(5, "Editori");

        var result = await new DeactivateRoleHandler(_roles, _unitOfWork)
            .Handle(new DeactivateRoleCommand(5), default);

        result.Succeeded.ShouldBeTrue();
        role.IsActive.ShouldBeFalse();
        await SavedOnce();
    }

    [Fact]
    public async Task Reactivate_RolDezactivat_IlReactiveaza()
    {
        var role = ExistingRole(5, "Editori", isActive: false);

        var result = await new ReactivateRoleHandler(_roles, _unitOfWork)
            .Handle(new ReactivateRoleCommand(5), default);

        result.Succeeded.ShouldBeTrue();
        role.IsActive.ShouldBeTrue();
        await SavedOnce();
    }

    [Fact]
    public async Task Reactivate_RolInexistent_EsteRefuzat()
    {
        _roles.GetForAdminAsync(99).Returns((AppRole?)null);

        var result = await new ReactivateRoleHandler(_roles, _unitOfWork)
            .Handle(new ReactivateRoleCommand(99), default);

        result.Succeeded.ShouldBeFalse();
        await NothingWasSaved();
    }

    // ------------------- REVOCARE -------------------

    [Fact]
    public async Task Revoke_DreptDeSistemDinAdmin_EsteRefuzat()
    {
        ExistingRole(2, SystemRoles.Admin);
        ExistingClaim(4, Permissions.Roles.Manage);
        ExistingLink(2, 4);

        var result = await new RevokeClaimFromRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new RevokeClaimFromRoleCommand(2, 4), default);

        result.Succeeded.ShouldBeFalse();
        _roles.DidNotReceive().RemoveClaim(Arg.Any<RoleClaim>());
        await NothingWasSaved();
    }

    [Fact]
    public async Task Revoke_DreptObisnuitDinAdmin_EstePermis()
    {
        ExistingRole(2, SystemRoles.Admin);
        ExistingClaim(9, "rapoarte.view");
        var link = ExistingLink(2, 9);

        var result = await new RevokeClaimFromRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new RevokeClaimFromRoleCommand(2, 9), default);

        result.Succeeded.ShouldBeTrue();
        _roles.Received(1).RemoveClaim(link);
        await SavedOnce();
    }

    [Fact]
    public async Task Revoke_DreptDeSistemDinAltRol_EstePermis()
    {
        // Protectia priveste doar rolul Admin: alte roluri pot primi si pierde orice drept.
        ExistingRole(5, "Editori");
        ExistingClaim(4, Permissions.Roles.Manage);
        var link = ExistingLink(5, 4);

        var result = await new RevokeClaimFromRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new RevokeClaimFromRoleCommand(5, 4), default);

        result.Succeeded.ShouldBeTrue();
        _roles.Received(1).RemoveClaim(link);
    }

    // ------------------- ACORDARE -------------------

    [Fact]
    public async Task Grant_DreptInexistent_EsteRefuzatInLocDeEroareDeCheieStraina()
    {
        ExistingRole(5, "Editori");
        _roles.GetRoleClaimAsync(5, 99).Returns((RoleClaim?)null);
        _claims.GetForAdminAsync(99).Returns((AppClaim?)null);

        var result = await new GrantClaimToRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new GrantClaimToRoleCommand(5, 99), default);

        result.Succeeded.ShouldBeFalse();
        _roles.DidNotReceive().AddClaim(Arg.Any<RoleClaim>());
        await NothingWasSaved();
    }

    [Fact]
    public async Task Grant_DreptDezactivat_EsteRefuzat()
    {
        ExistingRole(5, "Editori");
        ExistingClaim(9, "rapoarte.view", isActive: false);
        _roles.GetRoleClaimAsync(5, 9).Returns((RoleClaim?)null);

        var result = await new GrantClaimToRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new GrantClaimToRoleCommand(5, 9), default);

        result.Succeeded.ShouldBeFalse();
        _roles.DidNotReceive().AddClaim(Arg.Any<RoleClaim>());
    }

    [Fact]
    public async Task Grant_PeCaleaFericita_AdaugaLegatura()
    {
        ExistingRole(5, "Editori");
        ExistingClaim(9, "rapoarte.view");
        _roles.GetRoleClaimAsync(5, 9).Returns((RoleClaim?)null);

        var result = await new GrantClaimToRoleHandler(_roles, _claims, _unitOfWork)
            .Handle(new GrantClaimToRoleCommand(5, 9), default);

        result.Succeeded.ShouldBeTrue();
        _roles.Received(1).AddClaim(Arg.Is<RoleClaim>(rc => rc.RoleId == 5 && rc.ClaimId == 9));
        await SavedOnce();
    }
}
