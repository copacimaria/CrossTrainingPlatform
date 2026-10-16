using System.Net;

namespace CrossTraining.IntegrationTests.Web;

public class AuthorizationTests : IClassFixture<CrossTrainingWebApplicationFactory>
{
    private readonly CrossTrainingWebApplicationFactory _factory;

    public AuthorizationTests(CrossTrainingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Games/DisplayGamesBasedOnDomain")]
    [InlineData("/Profile/Index")]
    [InlineData("/Profile/ChangePassword")]
    [InlineData("/AppRoles/Index")]
    [InlineData("/AppClaims/Index")]
    [InlineData("/Domain/Index")]
    [InlineData("/AppRoles/SearchAvailableClaims?roleId=1&term=a")]
    public async Task PaginileProtejate_RedirectioneazaAnonimulCatreLogin(string path)
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        // TestServer intoarce Location ca URL absolut, deci comparam pe cale.
        var location = response.Headers.Location!;
        var absolute = location.IsAbsoluteUri ? location : new Uri(new Uri("http://localhost"), location);

        absolute.AbsolutePath.ShouldBe("/UserAuth/Login");
        absolute.Query.ShouldContain("ReturnUrl");
    }

    [Theory]
    [InlineData("/UserAuth/Login")]
    [InlineData("/UserAuth/Register")]
    [InlineData("/Home/Index")]
    public async Task PaginileAnonime_RaspundDirect(string path)
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
