using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using CrossTraining.Domain.Authorization;
using CrossTraining.Domain.Entities;
using CrossTraining.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Randeaza efectiv paginile din spatele autorizarii.
///
/// Motivul: compilatorul verifica interiorul unui view (etapa 4 a schimbat 24 de
/// directive <c>@model</c>), dar NU verifica daca modelul pe care controllerul il
/// pasa unui partial este de tipul pe care partialul il declara. Nepotrivirea aceea
/// apare doar la randare.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class AdminPagesRenderTests : IDisposable
{
    private readonly SqlServerFixture _sql;
    private readonly WebApplicationFactory<Program>? _factory;
    private readonly SeededData _seed;

    public AdminPagesRenderTests(SqlServerFixture sql)
    {
        _sql = sql;
        if (!_sql.IsAvailable)
            return;

        _seed = SeedAsync().GetAwaiter().GetResult();
        _factory = new AdminFactory(_sql.ConnectionString, _seed.UserId);
    }

    public static TheoryData<string> AdminPages() =>
    [
        "/AppClaims/Index",
        "/AppClaims/Create",
        "/AppRoles/Index",
        "/Domain/Index",
        "/Domain/Create",
        "/Games/DisplayGamesBasedOnDomain",
        "/Games/Create",
        "/Profile/Index",
        "/Profile/ChangePassword",
        "/Profile/ChangeDomain"
    ];

    [SkippableTheory]
    [MemberData(nameof(AdminPages))]
    public async Task PaginaSeRandeazaFaraEroare(string path)
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{path} nu s-a randat");
    }

    [SkippableFact]
    public async Task PaginileCareDepindDeUnRand_SeRandeazaCuDateReale()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Fiecare dintre acestea trece un model construit in controller catre un partial:
        // exact locul unde o nepotrivire de tip nu ar fi prinsa la compilare.
        var paths = new[]
        {
            $"/AppClaims/Details/{_seed.ClaimId}",
            $"/AppClaims/Edit/{_seed.ClaimId}",
            $"/AppClaims/Delete/{_seed.ClaimId}",
            $"/AppRoles/Details/{_seed.RoleId}",
            $"/AppRoles/Edit/{_seed.RoleId}",
            $"/AppRoles/Delete/{_seed.RoleId}",
            $"/AppRoles/AddClaim/{_seed.RoleId}",
            $"/Games/Edit/{_seed.GameId}",
            $"/Games/Delete/{_seed.GameId}",
            $"/Domain/Edit/{_seed.DomainId}",
            $"/Domain/Delete/{_seed.DomainId}"
        };

        foreach (var path in paths)
        {
            var response = await client.GetAsync(new Uri(path, UriKind.Relative));
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{path} nu s-a randat");
        }
    }

    // ------------------- ROLURI SI DREPTURI DEZACTIVATE / DE SISTEM -------------------

    [SkippableFact]
    public async Task RandurileDezactivate_ApardInListaCuButonDeReactivare()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = _factory!.CreateClient();

        var claims = await client.GetStringAsync(Path($"/AppClaims/Index?claimSearch={_seed.Suffix}"));
        var roles = await client.GetStringAsync(Path($"/AppRoles/Index?roleSearch={_seed.Suffix}"));

        claims.ShouldContain($"data-reactivate-claim=\"{_seed.InactiveClaimId}\"");
        roles.ShouldContain($"data-reactivate-role=\"{_seed.InactiveRoleId}\"");
    }

    [SkippableFact]
    public async Task DrepturileDeSistem_NuAuButoaneDeModificare()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = _factory!.CreateClient();

        var html = await client.GetStringAsync(Path($"/AppClaims/Index?claimSearch={Permissions.Roles.Manage}"));

        html.ShouldContain("Sistem");
        html.ShouldNotContain("loadEditClaim(");
        html.ShouldNotContain("loadDeleteClaim(");
    }

    [SkippableFact]
    public async Task RolulAdmin_NuPoateFiDezactivatDinEcran()
    {
        // Cererea pe care ar trimite-o butonul, chiar daca butonul nu mai e afisat:
        // protectia sta in cazul de utilizare, nu in view.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        int adminRoleId;
        await using (var db = _sql.CreateDbContext())
            adminRoleId = (await db.Roles.SingleAsync(r => r.RoleName == SystemRoles.Admin)).RoleId;

        using var client = _factory!.CreateClient();
        var response = await PostFromPageAsync(client, "/AppRoles/Index", "/AppRoles/DeleteConfirmed", adminRoleId);

        response.ShouldContain("\"success\":false");
        await using (var db = _sql.CreateDbContext())
            (await db.Roles.SingleAsync(r => r.RoleId == adminRoleId)).IsActive.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task DreptDezactivat_PoateFiReactivatDinEcran()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = _factory!.CreateClient();
        var response = await PostFromPageAsync(client, "/AppClaims/Index", "/AppClaims/Reactivate", _seed.InactiveClaimId);

        response.ShouldContain("\"success\":true");
        await using var db = _sql.CreateDbContext();
        (await db.Claims.SingleAsync(c => c.ClaimId == _seed.InactiveClaimId)).IsActive.ShouldBeTrue();
    }

    public void Dispose() => _factory?.Dispose();

    private static Uri Path(string relative) => new(relative, UriKind.Relative);

    /// <summary>
    /// Trimite un POST asa cum il trimite JS-ul din pagina: cu primul token antiforgery
    /// gasit in HTML. Pe o pagina autentificata il pune formularul de logout din layout,
    /// de unde il ia si <c>getToken()</c> cand nu e deschis niciun modal.
    /// </summary>
    private static async Task<string> PostFromPageAsync(HttpClient client, string pagePath, string actionPath, int id)
    {
        var page = await client.GetStringAsync(Path(pagePath));
        var token = Regex.Match(page, """name="__RequestVerificationToken"[^>]*value="([^"]+)""").Groups[1].Value;
        token.ShouldNotBeEmpty($"{pagePath} nu randeaza token-ul antiforgery");

        using var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("id", id.ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);

        var response = await client.PostAsync(Path(actionPath), body);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{actionPath} a raspuns {response.StatusCode}");
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<SeededData> SeedAsync()
    {
        await using var db = _sql.CreateDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var domain = TestData.Domain($"domeniu-{suffix}");
        var role = TestData.Role($"rol-{suffix}");
        var claim = TestData.Claim($"drept-{suffix}");
        db.Domains.Add(domain);
        db.Roles.Add(role);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        var user = TestData.User($"admin-{suffix}@test.local", role.RoleId);
        var game = TestData.Game($"joc-{suffix}", domain.DomainId, "descriere");
        db.Users.Add(user);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        db.RoleClaims.Add(new RoleClaim(role.RoleId, claim.ClaimId));
        db.UserDomains.Add(new UserDomain(user.UserId, domain.DomainId));

        // Cate un rand dezactivat, gasit in liste dupa acelasi sufix.
        var inactiveRole = TestData.Role($"rol-{suffix}-inactiv", isActive: false);
        var inactiveClaim = TestData.Claim($"drept-{suffix}-inactiv", isActive: false);
        db.Roles.Add(inactiveRole);
        db.Claims.Add(inactiveClaim);
        await db.SaveChangesAsync();

        return new SeededData(
            suffix, user.UserId, role.RoleId, claim.ClaimId, domain.DomainId, game.GameId,
            inactiveRole.RoleId, inactiveClaim.ClaimId);
    }

    private readonly record struct SeededData(
        string Suffix,
        int UserId,
        int RoleId,
        int ClaimId,
        int DomainId,
        int GameId,
        int InactiveRoleId,
        int InactiveClaimId);

    private sealed class AdminFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly int _userId;

        public AdminFactory(string connectionString, int userId)
        {
            _connectionString = connectionString;
            _userId = userId;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
            builder.UseSetting(TestAuthHandler.UserIdSetting, _userId.ToString(CultureInfo.InvariantCulture));

            builder.ConfigureTestServices(services =>
            {
                // Ruleaza dupa configurarea aplicatiei, deci schema implicita devine
                // aceasta si cookie-ul de autentificare nu mai e cerut.
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        }
    }
}
