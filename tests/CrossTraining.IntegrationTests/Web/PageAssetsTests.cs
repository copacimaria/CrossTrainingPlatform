using System.Net;
using System.Text.RegularExpressions;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Fiecare stylesheet si script referit de layout trebuie sa existe. Un fisier lipsa nu
/// produce nicio eroare pe server; in browser apare doar in consola, iar pentru un
/// vizitator neautentificat cererea era chiar redirectionata spre login (politica implicita
/// cere autentificare si pentru un fisier static inexistent).
/// </summary>
public partial class PageAssetsTests : IClassFixture<CrossTrainingWebApplicationFactory>
{
    private readonly CrossTrainingWebApplicationFactory _factory;

    public PageAssetsTests(CrossTrainingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Home/Index")]
    [InlineData("/UserAuth/Login")]
    [InlineData("/UserAuth/Register")]
    public async Task ResurseleReferiteDePagina_Exista(string path)
    {
        using var client = _factory.CreateNonRedirectingClient();
        var html = await client.GetStringAsync(new Uri(path, UriKind.Relative));

        var assets = LocalAsset().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        assets.ShouldNotBeEmpty();

        foreach (var asset in assets)
        {
            var response = await client.GetAsync(new Uri(asset, UriKind.Relative));
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{path} refera {asset}, care nu exista");
        }
    }

    [GeneratedRegex("""<(?:link|script)\b[^>]*\b(?:href|src)="(/[^"]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex LocalAsset();
}
