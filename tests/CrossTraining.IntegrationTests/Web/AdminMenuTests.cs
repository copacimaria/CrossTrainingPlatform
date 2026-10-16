using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CrossTraining.Domain.Authorization;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Meniul de administrare se afiseaza dupa drepturi, nu dupa numele rolului.
///
/// Inainte, meniul cerea rolul "Admin": un rol nou cu roles.view nu vedea linkul catre
/// pagina pe care avea voie sa o deschida, iar un Admin caruia i se scosesera drepturile
/// vedea linkuri care duceau la "acces interzis".
///
/// Pagina folosita, /Profile/ChangePassword, nu atinge baza de date, deci testele nu au
/// nevoie de SQL Server.
/// </summary>
public sealed class AdminMenuTests
{
    private const string AdminMenu = "Admin Settings";
    private const string RolesLink = "href=\"/AppRoles/Index\"";
    private const string ClaimsLink = "href=\"/AppClaims/Index\"";
    private const string DomainsLink = "href=\"/Domain/Index\"";

    [Fact]
    public async Task FaraDrepturi_NuApareMeniulDeAdministrare()
    {
        var html = await RenderLayoutAsync(role: null);

        html.ShouldNotContain(AdminMenu);
    }

    [Fact]
    public async Task RolulAdminFaraDrepturi_NuMaiVedeMeniul()
    {
        // Regresia exacta: meniul se lua dupa User.IsInRole("Admin").
        var html = await RenderLayoutAsync(role: SystemRoles.Admin);

        html.ShouldNotContain(AdminMenu);
    }

    [Fact]
    public async Task UnSingurDrept_ArataDoarLinkulLui()
    {
        var html = await RenderLayoutAsync(role: "Auditor", Permissions.Claims.View);

        html.ShouldContain(AdminMenu);
        html.ShouldContain(ClaimsLink);
        html.ShouldNotContain(RolesLink);
        html.ShouldNotContain(DomainsLink);
    }

    [Fact]
    public async Task ToateDrepturileDeVizualizare_ArataToateLinkurile()
    {
        var html = await RenderLayoutAsync(
            role: "Auditor",
            Permissions.Roles.View,
            Permissions.Claims.View,
            Permissions.Domains.View);

        html.ShouldContain(RolesLink);
        html.ShouldContain(ClaimsLink);
        html.ShouldContain(DomainsLink);
    }

    private static async Task<string> RenderLayoutAsync(string? role, params string[] permissions)
    {
        using var factory = new ConfiguredUserFactory(role, permissions);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(new Uri("/Profile/ChangePassword", UriKind.Relative));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private sealed class ConfiguredUserFactory : WebApplicationFactory<Program>
    {
        private readonly string? _role;
        private readonly string[] _permissions;

        public ConfiguredUserFactory(string? role, string[] permissions)
        {
            _role = role;
            _permissions = permissions;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                @"Server=(localdb)\nu-se-conecteaza;Database=Test;Trusted_Connection=True");
            builder.UseSetting(ConfiguredUserAuthHandler.RoleSetting, _role ?? string.Empty);
            builder.UseSetting(ConfiguredUserAuthHandler.PermissionsSetting, string.Join(',', _permissions));

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(ConfiguredUserAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, ConfiguredUserAuthHandler>(
                        ConfiguredUserAuthHandler.SchemeName, _ => { });
            });
        }
    }

    /// <summary>Autentifica un utilizator cu rolul si drepturile date prin configuratie.</summary>
    private sealed class ConfiguredUserAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ConfiguredUser";
        public const string RoleSetting = "MenuTest:Role";
        public const string PermissionsSetting = "MenuTest:Permissions";

        private readonly IConfiguration _configuration;

        public ConfiguredUserAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "1"),
                new(ClaimTypes.Name, "Utilizator De Test"),
                new(
                    UserPrincipalFactory.RefreshedAtClaim,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
            };

            var role = _configuration[RoleSetting];
            if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var permissions = (_configuration[PermissionsSetting] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            claims.AddRange(permissions.Select(p => new Claim(PermissionClaim.Type, p)));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
