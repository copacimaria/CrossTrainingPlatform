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
        csp.ShouldContain("script-src 'self'");
    }

    [Fact]
    public async Task RaspunsurileNuDezvaluieServerul()
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri("/Home/Index", UriKind.Relative));

        response.Headers.Contains("Server").ShouldBeFalse();
    }
}
