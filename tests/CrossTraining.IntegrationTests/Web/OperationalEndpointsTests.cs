using System.Globalization;
using System.Net;
using System.Text.Json;
using CrossTraining.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Partea operationala a aplicatiei: verificarile de sanatate si raspunsul la erori
/// neasteptate. Nu au nevoie de SQL Server - baza "lipsa" este chiar scenariul testat.
/// </summary>
public sealed class OperationalEndpointsTests : IDisposable
{
    private readonly ThrowingEndpointFactory _factory = new();

    [Fact]
    public async Task Health_RaspundeFaraAutentificare()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthReady_FaraBazaDeDate_SpuneCaAplicatiaNuPoateServiCereri()
    {
        // Procesul e pornit, dar fara baza de date nu poate servi nimic: orchestratorul
        // nu trebuie sa-i trimita trafic.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
    }

    [Fact]
    public async Task EroareInCerereAjax_PrimesteProblemDetailsPeCareJsLPoateAfisa()
    {
        // Inainte, JS-ul primea pagina HTML de eroare, nu o putea citi si afisa
        // "Eroare necunoscuta". Acum primeste JSON cu mesaj si cu codul cererii.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ThrowingTestController.Route, UriKind.Relative));
        request.Headers.Add("RequestVerificationToken", "orice");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        root.GetProperty("success").GetBoolean().ShouldBeFalse();
        root.GetProperty("errors")[0].GetString().ShouldNotBeNull().ShouldContain("Codul pentru suport");
        root.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        // Detaliile exceptiei nu ajung la client.
        root.ToString().ShouldNotContain(ThrowingTestController.Message);
    }

    [Fact]
    public async Task EroareInCererePagina_AjungeLaPaginaDeEroareCuCodulCererii()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri(ThrowingTestController.Route, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Request ID");
        html.ShouldNotContain(ThrowingTestController.Message);
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>Aplicatia reala, plus un controller de test care arunca o exceptie.</summary>
    private sealed class ThrowingEndpointFactory : CrossTrainingWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
                services.AddControllersWithViews().AddApplicationPart(typeof(ThrowingTestController).Assembly));
        }
    }
}

/// <summary>
/// Verificarea de disponibilitate pe o baza de date reala: cand baza raspunde, aplicatia
/// se declara gata.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ReadinessWithDatabaseTests
{
    private readonly SqlServerFixture _sql;

    public ReadinessWithDatabaseTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task HealthReady_CuBazaDeDate_RaspundeHealthy()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        using var factory = new DatabaseFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed class DatabaseFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public DatabaseFactory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        }
    }
}

/// <summary>Controller folosit doar in teste: arunca o exceptie, ca sa se vada raspunsul aplicatiei.</summary>
[AllowAnonymous]
public sealed class ThrowingTestController : Controller
{
    public const string Route = "/test/arunca";
    public const string Message = "Detaliu intern care nu trebuie sa ajunga la client";

    [HttpGet(Route)]
    public IActionResult Throw()
        => throw new InvalidOperationException(
            string.Create(CultureInfo.InvariantCulture, $"{Message} ({Request.Path})"));
}
