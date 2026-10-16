using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Porneste aplicatia reala in memorie, cu pipeline-ul complet: autorizare, rate
/// limiting, headere de securitate, rutare.
///
/// Nu se conecteaza la o baza de date. Connection string-ul este unul fictiv, doar cat
/// sa treaca validarea de la pornire: testele de aici verifica pipeline-ul, nu accesul
/// la date. Interogarile sunt acoperite de testele din folderul Persistence.
/// </summary>
public class CrossTrainingWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string UnusedConnectionString =
        @"Server=(localdb)\nu-se-conecteaza;Database=Test;Trusted_Connection=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Mediu propriu: user-secrets-urile de dezvoltare nu trebuie sa influenteze testele.
        builder.UseEnvironment("Testing");

        // UseSetting, nu ConfigureAppConfiguration: Program.cs citeste
        // builder.Configuration in timpul construirii aplicatiei, adica inainte ca
        // hook-urile de ConfigureAppConfiguration ale factory-ului sa apuce sa ruleze.
        builder.UseSetting("ConnectionStrings:DefaultConnection", UnusedConnectionString);
    }

    /// <summary>Client care nu urmareste redirectarile, ca sa vedem exact ce raspunde serverul.</summary>
    public HttpClient CreateNonRedirectingClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
