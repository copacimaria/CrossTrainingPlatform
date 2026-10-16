using System.Globalization;
using System.Threading.RateLimiting;
using CrossTraining.Application.DependencyInjection;
using CrossTraining.Infrastructure.DependencyInjection;
using CrossTraining.Web.Middleware;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Kestrel nu isi mai anunta identitatea in fiecare raspuns. Se face aici, nu din
// middleware: headerul "Server" e scris de Kestrel la trimiterea raspunsului, deci
// un Headers.Remove() din pipeline nu are ce sterge.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// MVC
builder.Services.AddControllersWithViews();

// Straturile Application + Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

// Semnarea id-ului de utilizator in fluxul de inregistrare (vezi RegistrationTokenProtector).
// TODO(productie): cheile de Data Protection trebuie persistate intr-o locatie partajata,
// altfel token-urile si cookie-urile devin invalide la fiecare repornire sau pe alt nod.
builder.Services.AddSingleton<RegistrationTokenProtector>();
builder.Services.AddScoped<RefreshUserPrincipal>();

// Autentificare pe baza de cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/UserAuth/Login";
        options.LogoutPath = "/UserAuth/Logout";
        options.AccessDeniedPath = "/UserAuth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;

        options.Cookie.Name = "CrossTraining.Auth";
        options.Cookie.HttpOnly = true;              // inaccesibil din JavaScript
        options.Cookie.IsEssential = true;           // nu depinde de acceptul de cookie-uri
        options.Cookie.SameSite = SameSiteMode.Lax;  // nu pleaca la cereri cross-site
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // doar peste HTTPS

        // Reciteste rolul si drepturile din baza la interval scurt. Fara asta, o
        // permisiune revocata ar ramane valabila pana la expirarea cookie-ului.
        options.Events.OnValidatePrincipal = context =>
            context.HttpContext.RequestServices
                   .GetRequiredService<RefreshUserPrincipal>()
                   .ValidateAsync(context);
    });

// Implicit, orice endpoint cere utilizator autentificat. Exceptiile se marcheaza
// explicit cu [AllowAnonymous]. Asa, un controller nou adaugat este protejat din
// oficiu - varianta anterioara, cu [Authorize] pus manual pe fiecare controller,
// era deschisa daca cineva uita atributul.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Autorizare pe drepturi, nu pe roluri hardcodate. Politicile se construiesc la cerere
// din numele dreptului, deci nu exista o lista de inregistrat si de tinut sincronizata.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Limitare de rata pe endpoint-urile de autentificare. Fara ea, atacul prin
// incercari repetate de parola nu intampina nicio rezistenta.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Authentication, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Prea multe incercari. Reincercati peste cateva minute.", cancellationToken);
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Dupa UseExceptionHandler, intentionat: cand acela prinde o exceptie face
// Response.Clear() si re-executa pipeline-ul de aici incolo, deci headerele
// se rescriu si pe raspunsul de eroare.
app.UseSecurityHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=UserAuth}/{action=Register}/{id?}");

app.Run();

/// <summary>
/// Expusa explicit ca sa poata fi folosita drept punct de intrare de
/// WebApplicationFactory in testele de integrare. Top-level statements genereaza
/// altfel o clasa Program interna, invizibila din proiectul de test.
/// </summary>
public partial class Program;
