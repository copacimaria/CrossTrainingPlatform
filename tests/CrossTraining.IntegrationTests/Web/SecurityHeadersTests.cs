using System.Net;

namespace CrossTraining.IntegrationTests.Web;

public class SecurityHeadersTests : IClassFixture<CrossTrainingWebApplicationFactory>
{
    private readonly CrossTrainingWebApplicationFactory _factory;

    public SecurityHeadersTests(CrossTrainingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Content-Security-Policy")]
    [InlineData("X-Content-Type-Options")]
    [InlineData("X-Frame-Options")]
    [InlineData("Referrer-Policy")]
    [InlineData("Permissions-Policy")]
    public async Task RaspunsurileAuHeadereleDeSecuritate(string header)
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Home/Index", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains(header).ShouldBeTrue($"lipseste headerul {header}");
    }

    [Fact]
    public async Task CspulBlocheazaContinutDinAlteOrigini()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Home/Index", UriKind.Relative));
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("object-src 'none'");
        csp.ShouldContain("frame-ancestors 'none'");
        csp.ShouldContain("form-action 'self'");
    }

    [Fact]
    public async Task CspulRuleazaDoarScripturiDinFisiereleAplicatiei()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Home/Index", UriKind.Relative));
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();

        // Fara 'unsafe-inline' si fara alte surse: un script injectat in pagina nu ruleaza.
        csp.Split(';', StringSplitOptions.TrimEntries)
           .Single(d => d.StartsWith("script-src", StringComparison.Ordinal))
           .ShouldBe("script-src 'self'");
    }

    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/UserAuth/Login")]
    [InlineData("/UserAuth/Register")]
    public async Task PaginileAnonime_NuAuScripturiInline(string path)
    {
        // Paginile din spatele autentificarii sunt verificate in AdminPagesRenderTests.
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        InlineScriptScanner.FindViolations(await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task RaspunsurileNuDezvaluieServerul()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Home/Index", UriKind.Relative));

        response.Headers.Contains("Server").ShouldBeFalse();
    }
}
