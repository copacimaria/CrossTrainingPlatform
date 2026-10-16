using System.Net;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// IClassFixture da o instanta de factory proprie acestei clase. Conteaza: limitatorul
/// de rata e singleton, deci un factory partajat ar consuma bugetul altor teste.
/// </summary>
public class RateLimitingTests : IClassFixture<CrossTrainingWebApplicationFactory>
{
    private const int PermitLimit = 10;

    private readonly CrossTrainingWebApplicationFactory _factory;

    public RateLimitingTests(CrossTrainingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_DupaDepasireaLimitei_Raspunde429CuRetryAfter()
    {
        using var client = _factory.CreateNonRedirectingClient();

        // Primele cereri trec de limitator. Ca sunt respinse mai departe de antiforgery
        // nu conteaza aici: ne intereseaza doar sa nu fie 429.
        for (int i = 0; i < PermitLimit; i++)
        {
            using var allowed = await PostLoginAsync(client);
            allowed.StatusCode.ShouldNotBe(
                HttpStatusCode.TooManyRequests,
                $"cererea {i + 1} nu ar fi trebuit limitata");
        }

        using var rejected = await PostLoginAsync(client);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client)
    {
        using var body = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", "atacator@example.com"),
            new KeyValuePair<string, string>("Password", "incercare")
        ]);

        return await client.PostAsync(new Uri("/UserAuth/Login", UriKind.Relative), body);
    }
}
