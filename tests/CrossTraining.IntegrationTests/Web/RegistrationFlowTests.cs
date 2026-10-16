using System.Net;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Regresie pentru IDOR-ul descris in ADR-0004: pasul de alegere a domeniilor accepta
/// doar un token semnat, nu un id de utilizator trimis de client.
/// </summary>
public class RegistrationFlowTests : IClassFixture<CrossTrainingWebApplicationFactory>
{
    private readonly CrossTrainingWebApplicationFactory _factory;

    public RegistrationFlowTests(CrossTrainingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Domain/SelectDomain?userId=1")]        // vechiul mod de acces
    [InlineData("/Domain/SelectDomain")]                 // fara nimic
    [InlineData("/Domain/SelectDomain?token=")]          // token gol
    [InlineData("/Domain/SelectDomain?token=AAAAbogus")] // token fabricat
    public async Task SelectDomain_FaraTokenValid_TrimiteInapoiLaRegister(string path)
    {
        using var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.ShouldNotContain("SelectDomain");
    }

    [Fact]
    public async Task SaveUserDomain_FaraAntiforgery_EsteRespins()
    {
        using var client = _factory.CreateNonRedirectingClient();

        using var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("token", "AAAAbogus"),
            new KeyValuePair<string, string>("domainIds", "1")
        ]);

        var response = await client.PostAsync(new Uri("/Domain/SaveUserDomain", UriKind.Relative), body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
