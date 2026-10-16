using System.Net;
using System.Text.RegularExpressions;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Domain.Authorization;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;
using CrossTraining.IntegrationTests.Persistence;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Revalidarea identitatii din cookie este un control de securitate: fara ea, o
/// permisiune revocata sau un cont dezactivat ar continua sa functioneze pana la
/// expirarea cookie-ului.
///
/// Testele parcurg fluxul real - login prin formular, cookie emis de aplicatie - si
/// folosesc un interval de reimprospatare de zero, ca sa nu astepte cinci minute.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class PrincipalRefreshTests : IDisposable
{
    private const string Password = "parola-de-test-123";

    private readonly SqlServerFixture _sql;
    private readonly WebApplicationFactory<Program>? _factory;
    private readonly string _email = $"refresh-{Guid.NewGuid():N}@test.local";
    private readonly int _roleId;

    public PrincipalRefreshTests(SqlServerFixture sql)
    {
        _sql = sql;
        if (!_sql.IsAvailable)
            return;

        _factory = new ImmediateRefreshFactory(_sql.ConnectionString);
        _roleId = SeedUserAsync().GetAwaiter().GetResult();
    }

    [SkippableFact]
    public async Task DreptRevocat_NuMaiEsteValabilDupaReimprospatare()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = CreateClient();
        await LoginAsync(client);

        // Cu dreptul acordat, pagina raspunde.
        (await client.GetAsync(Path("/AppRoles/Index"))).StatusCode.ShouldBe(HttpStatusCode.OK);

        await RevokeAllPermissionsAsync();

        // Aceeasi sesiune, acelasi cookie: dreptul dispare la urmatoarea cerere.
        var afterRevoke = await client.GetAsync(Path("/AppRoles/Index"));
        afterRevoke.StatusCode.ShouldBe(HttpStatusCode.Found);
        LocationPath(afterRevoke).ShouldBe("/UserAuth/AccessDenied");
    }

    [SkippableFact]
    public async Task ContDezactivat_EsteDeconectat()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var client = CreateClient();
        await LoginAsync(client);

        (await client.GetAsync(Path("/Profile/ChangePassword"))).StatusCode.ShouldBe(HttpStatusCode.OK);

        await DeactivateUserAsync();

        // Sesiunea este respinsa, deci utilizatorul ajunge inapoi la login.
        var afterDeactivate = await client.GetAsync(Path("/Profile/ChangePassword"));
        afterDeactivate.StatusCode.ShouldBe(HttpStatusCode.Found);
        LocationPath(afterDeactivate).ShouldBe("/UserAuth/Login");
    }

    public void Dispose() => _factory?.Dispose();

    // ------------------- helperi -------------------

    private HttpClient CreateClient()
        => _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,

            // Cookie-ul de autentificare este marcat Secure, deci nu ar fi trimis inapoi
            // peste http. TestServer trateaza cererea ca fiind HTTPS pe baza acestei adrese.
            BaseAddress = new Uri("https://localhost")
        });

    private static Uri Path(string relative) => new(relative, UriKind.Relative);

    private static string LocationPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location!;
        var absolute = location.IsAbsoluteUri ? location : new Uri(new Uri("http://localhost"), location);
        return absolute.AbsolutePath;
    }

    private async Task LoginAsync(HttpClient client)
    {
        var page = await client.GetStringAsync(Path("/UserAuth/Login"));
        var token = Regex.Match(page, """name="__RequestVerificationToken"[^>]*value="([^"]+)""").Groups[1].Value;
        token.ShouldNotBeEmpty("nu am gasit token-ul antiforgery in pagina de login");

        using var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", _email),
            new KeyValuePair<string, string>("Password", Password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]);

        var response = await client.PostAsync(Path("/UserAuth/Login"), body);
        response.StatusCode.ShouldBe(HttpStatusCode.Found, "login-ul nu a reusit");
    }

    private async Task<int> SeedUserAsync()
    {
        await using var db = _sql.CreateDbContext();

        // Rol propriu, nu rolul Admin partajat: testul de revocare sterge toate
        // drepturile rolului, iar clasele din aceasta colectiune ruleaza pe aceeasi baza.
        var role = TestData.Role($"refresh-{Guid.NewGuid():N}"[..40]);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var rolesViewClaimId = await db.Claims.IgnoreQueryFilters()
            .Where(c => c.ClaimName == Permissions.Roles.View)
            .Select(c => c.ClaimId)
            .FirstAsync();

        db.RoleClaims.Add(new RoleClaim(role.RoleId, rolesViewClaimId));

        var user = TestData.User(_email, role.RoleId);

        // Hash-ul real: login-ul trece prin verificarea Argon2, nu printr-un ocol de test.
        IPasswordService passwords = _factory!.Services.GetRequiredService<IPasswordService>();
        user.SetPasswordHash(passwords.Hash(user, Password));

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return role.RoleId;
    }

    private async Task RevokeAllPermissionsAsync()
    {
        await using var db = _sql.CreateDbContext();
        var links = await db.RoleClaims.IgnoreQueryFilters()
            .Where(rc => rc.RoleId == _roleId)
            .ToListAsync();

        db.RoleClaims.RemoveRange(links);
        await db.SaveChangesAsync();
    }

    private async Task DeactivateUserAsync()
    {
        await using var db = _sql.CreateDbContext();
        var email = Email.Create(_email).Value;
        var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
        user.Deactivate();
        await db.SaveChangesAsync();
    }

    /// <summary>Reimprospateaza identitatea la fiecare cerere, ca testul sa nu astepte.</summary>
    private sealed class ImmediateRefreshFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public ImmediateRefreshFactory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
            builder.UseSetting(RefreshUserPrincipal.IntervalSettingKey, "0");
        }
    }
}
