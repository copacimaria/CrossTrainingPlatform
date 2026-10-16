using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Autorizarea trebuie sa refuze, nu doar sa permita.
///
/// Un utilizator autentificat dar fara dreptul cerut nu are ce cauta in ecranele de
/// administrare. Fara acest test, o politica scrisa gresit ar trece neobservata: testele
/// care ruleaza cu toate drepturile ar fi verzi oricum.
/// </summary>
public sealed class PermissionEnforcementTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new NoPermissionsFactory();

    [Theory]
    [InlineData("/AppClaims/Index")]
    [InlineData("/AppRoles/Index")]
    [InlineData("/Domain/Index")]
    [InlineData("/Games/Create")]
    public async Task UtilizatorFaraDrept_PrimesteAccesInterzis(string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        // 403 este decizia de autorizare in sine. In aplicatia reala, handlerul de cookie
        // o traduce intr-o redirectare catre /UserAuth/AccessDenied; schema de test nu
        // face acea traducere, iar verificarea codului este oricum mai precisa.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UtilizatorFaraDrept_AreVoieLaPaginileNeprotejate()
    {
        // Contra-proba: refuzul de mai sus vine din politica de drepturi, nu din faptul
        // ca utilizatorul de test nu ar fi autentificat deloc.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(new Uri("/Profile/ChangePassword", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>Autentifica un utilizator obisnuit: fara rol si fara niciun drept.</summary>
    private sealed class NoPermissionsFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                @"Server=(localdb)\nu-se-conecteaza;Database=Test;Trusted_Connection=True");

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(PlainUserAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, PlainUserAuthHandler>(
                        PlainUserAuthHandler.SchemeName, _ => { });
            });
        }
    }

    private sealed class PlainUserAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "PlainUser";

        public PlainUserAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Name, "Utilizator Simplu"),

                    // Marcajul de reimprospatare este proaspat, ca revalidarea sa nu
                    // incerce sa citeasca din baza de date - testul verifica autorizarea.
                    new Claim(
                        UserPrincipalFactory.RefreshedAtClaim,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(
                            System.Globalization.CultureInfo.InvariantCulture))
                ],
                SchemeName);

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
