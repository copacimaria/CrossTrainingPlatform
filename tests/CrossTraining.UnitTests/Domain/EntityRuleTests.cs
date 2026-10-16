using CrossTraining.Domain.Authorization;
using CrossTraining.Domain.Common;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.UnitTests.Domain;

// Regulile de business stau in entitati (ADR-0010), deci se pot verifica direct, fara
// baza de date, fara mock-uri si fara pipeline. Testele de aici sunt specificatia lor.

public class EmailTests
{
    [Fact]
    public void Create_NormalizeazaSpatiileSiLiterele()
        => Email.Create("  Ana.Pop@Exemplu.RO ").Value.Value.ShouldBe("ana.pop@exemplu.ro");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fara-arond")]
    [InlineData("@exemplu.ro")]
    [InlineData("ana@")]
    [InlineData("ana@exemplu")]
    [InlineData("ana@@exemplu.ro")]
    [InlineData("ana pop@exemplu.ro")]
    [InlineData("ana@.ro")]
    public void Create_RefuzaFormateleInvalide(string input)
        => Email.Create(input).Succeeded.ShouldBeFalse();

    [Fact]
    public void Create_RefuzaAdreseleMaiLungiDecatColoana()
        => Email.Create(new string('a', Email.MaxLength) + "@x.ro").Succeeded.ShouldBeFalse();

    [Fact]
    public void DouaScrieriAleAceleiasiAdrese_SuntEgale()
        => Email.Create("ANA@exemplu.ro").Value.ShouldBe(Email.Create("ana@exemplu.ro").Value);
}

public class DomainResultTests
{
    [Fact]
    public void ValoareaUnuiRefuz_NuPoateFiCitita()
        => Should.Throw<InvalidOperationException>(() => DomainResult.Fail<string>("motiv").Value);
}

public class AppClaimRuleTests
{
    [Theory]
    [InlineData("roles.manage")]
    [InlineData("Claims.View")]
    [InlineData("")]
    public void Create_RefuzaNumeleRezervateSauGoale(string name)
        => AppClaim.Create(name).Succeeded.ShouldBeFalse();

    [Fact]
    public void Create_RefuzaNumeleMaiLungiDecatColoana()
        => AppClaim.Create(new string('x', AppClaim.NameMaxLength + 1)).Succeeded.ShouldBeFalse();

    [Fact]
    public void Create_PastreazaNumeleFaraSpatiiLaCapete()
        => AppClaim.Create("  rapoarte.view ").Value.ClaimName.ShouldBe("rapoarte.view");

    [Fact]
    public void DreptDeSistem_NuPoateFiRedenumitSauDezactivat()
    {
        var claim = Loaded.Claim(1, Permissions.Roles.Manage);

        claim.Rename("altceva").Succeeded.ShouldBeFalse();
        claim.Deactivate().Succeeded.ShouldBeFalse();

        claim.ClaimName.ShouldBe(Permissions.Roles.Manage);
        claim.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void DreptObisnuit_PoateFiDezactivatSiReactivat()
    {
        var claim = AppClaim.Create("rapoarte.view").Value;

        claim.Deactivate().Succeeded.ShouldBeTrue();
        claim.IsActive.ShouldBeFalse();

        claim.Reactivate();
        claim.IsActive.ShouldBeTrue();
    }
}

public class AppRoleRuleTests
{
    [Fact]
    public void Admin_NuPierdeUnDreptDeSistem()
    {
        var admin = Loaded.Role(2, SystemRoles.Admin, claimIds: [4]);
        var rolesManage = Loaded.Claim(4, Permissions.Roles.Manage);

        admin.Revoke(rolesManage).Succeeded.ShouldBeFalse();

        admin.RoleClaims.Select(rc => rc.ClaimId).ShouldBe([4]);
    }

    [Fact]
    public void Grant_RefuzaUnDreptDezactivat()
    {
        var role = AppRole.Create("Editori").Value;

        role.Grant(Loaded.Claim(9, "rapoarte.view", isActive: false)).Succeeded.ShouldBeFalse();

        role.RoleClaims.ShouldBeEmpty();
    }

    [Fact]
    public void Grant_SiRevoke_SuntIdempotente()
    {
        var role = Loaded.Role(5, "Editori");
        var claim = Loaded.Claim(9, "rapoarte.view");

        role.Grant(claim).Succeeded.ShouldBeTrue();
        role.Grant(claim).Succeeded.ShouldBeTrue();
        role.RoleClaims.Count.ShouldBe(1);

        role.Revoke(claim).Succeeded.ShouldBeTrue();
        role.Revoke(claim).Succeeded.ShouldBeTrue();
        role.RoleClaims.ShouldBeEmpty();
    }
}

public class GameRuleTests
{
    private static DomainResult<Game> CreateWithLink(string? link)
        => Game.Create("Joc", null, link, domainId: null, DateTime.UtcNow);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://exemplu.ro/joc.zip")]
    [InlineData("jjj")]
    [InlineData("/descarcari/joc.zip")]
    public void LinkulDeDescarcare_TrebuieSaFieHttpSauHttps(string link)
        => CreateWithLink(link).Succeeded.ShouldBeFalse();

    [Theory]
    [InlineData("https://exemplu.ro/joc.zip")]
    [InlineData("http://exemplu.ro/joc")]
    public void LinkurileHttpSiHttps_SuntAcceptate(string link)
        => CreateWithLink(link).Value.DownloadLink.ShouldBe(link);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FaraLink_JoculNuAreLink(string? link)
        => CreateWithLink(link).Value.DownloadLink.ShouldBeNull();

    [Fact]
    public void DescriereaPreaLunga_EsteRefuzata()
        => Game.Create("Joc", new string('x', Game.DescriptionMaxLength + 1), null, null, DateTime.UtcNow)
               .Succeeded.ShouldBeFalse();

    [Fact]
    public void Update_Refuzat_NuSchimbaNimic()
    {
        var game = Game.Create("Joc", "descriere", "https://exemplu.ro", null, DateTime.UtcNow).Value;

        game.Update("Alt nume", "alta descriere", "javascript:alert(1)", 10).Succeeded.ShouldBeFalse();

        game.GameName.ShouldBe("Joc");
        game.Description.ShouldBe("descriere");
        game.DomainId.ShouldBeNull();
    }
}

public class AppUserRuleTests
{
    private static AppUser NewUser()
        => AppUser.Register("Ana", "Popescu", Email.Create("ana@exemplu.ro").Value, roleId: 1, DateTime.UtcNow).Value;

    [Theory]
    [InlineData("", "Popescu")]
    [InlineData("Ana", "  ")]
    public void Register_CereNumeSiPrenume(string name, string familyName)
        => AppUser.Register(name, familyName, Email.Create("ana@exemplu.ro").Value, 1, DateTime.UtcNow)
                  .Succeeded.ShouldBeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void Parola_AreLungimeMinima(string? password)
        => AppUser.ValidatePassword(password).Succeeded.ShouldBeFalse();

    [Fact]
    public void ChooseDomains_CereCelPutinUnDomeniu()
        => NewUser().ChooseDomains([]).Succeeded.ShouldBeFalse();

    [Fact]
    public void ChooseDomains_InlocuiesteListaFaraDuplicate()
    {
        var user = NewUser();

        user.ChooseDomains([10, 20, 20]).Succeeded.ShouldBeTrue();
        user.ChooseDomains([20, 30]).Succeeded.ShouldBeTrue();

        user.UserDomains.Select(ud => ud.DomainId).ShouldBe([20, 30], ignoreOrder: true);
    }

    [Fact]
    public void SetPasswordHash_RefuzaUnHashGol()
        => Should.Throw<ArgumentException>(() => NewUser().SetPasswordHash(" "));
}

public class TrainingDomainRuleTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NumeleEsteObligatoriu(string name)
        => TrainingDomain.Create(name, isActive: true).Succeeded.ShouldBeFalse();

    [Fact]
    public void Rename_Refuzat_PastreazaNumeleVechi()
    {
        var domain = TrainingDomain.Create("Matematica", isActive: true).Value;

        domain.Rename(new string('x', TrainingDomain.NameMaxLength + 1)).Succeeded.ShouldBeFalse();

        domain.DomainName.ShouldBe("Matematica");
    }
}
