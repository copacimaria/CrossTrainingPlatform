using System.Security.Claims;
using System.Text.Encodings.Web;
using CrossTraining.Application.Authorization;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Autentifica orice cerere ca administrator cu toate drepturile, ca sa putem randa
/// paginile din spatele autorizarii. Id-ul utilizatorului vine din configuratie, ca sa
/// corespunda unui rand real din baza de test.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    /// <summary>Cheia de configuratie prin care testul spune ca cine sa se autentifice.</summary>
    public const string UserIdSetting = "TestAuth:UserId";

    private readonly IConfiguration _configuration;

    public TestAuthHandler(
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
        var userId = _configuration[UserIdSetting] ?? "1";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Admin De Test"),
            new(ClaimTypes.Email, "admin@test.local"),
            new(ClaimTypes.Role, "Admin")
        };

        // Toate drepturile: simuleaza un administrator caruia migrarea SeedPermissions
        // i le-a acordat deja. Ca acordarea aceea chiar se intampla in baza de date este
        // verificat separat, in PermissionSeedTests.
        claims.AddRange(Permissions.All.Select(p => new Claim(PermissionClaim.Type, p)));

        var identity = new ClaimsIdentity(claims, SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
