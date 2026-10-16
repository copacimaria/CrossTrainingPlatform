using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using CrossTraining.Application.DependencyInjection;
using CrossTraining.Infrastructure.DependencyInjection;
using CrossTraining.Web.Middleware;
using CrossTraining.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Kestrel nu isi mai anunta identitatea in fiecare raspuns. Se face aici, nu din
// middleware: headerul "Server" e scris de Kestrel la trimiterea raspunsului, deci
// un Headers.Remove() din pipeline nu are ce sterge.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ------------------- LOGGING -------------------
// Serilog, cu evenimente structurate: proprietatile (UseCase, ElapsedMs, TraceId...) raman
// campuri, nu text, deci se pot filtra. Nivelurile vin din sectiunea "Serilog" din
// appsettings. preserveStaticLogger: fiecare host are logger-ul lui - testele pornesc
// multe aplicatii in acelasi proces.
builder.Services.AddSerilog(
    (services, logger) =>
    {
        logger.ReadFrom.Configuration(builder.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext();

        // Local: text lizibil. In rest: JSON compact, cate un eveniment pe linie, gata de
        // colectat de orice sistem de agregare a log-urilor.
        if (builder.Environment.IsDevelopment())
        {
            logger.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        }
    },
    preserveStaticLogger: true);

// ------------------- ERORI -------------------
// Erorile neasteptate din cererile trimise de JS primesc un ProblemDetails (JSON);
// cererile de pagina ajung la /Home/Error. Vezi AjaxExceptionHandler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AjaxExceptionHandler>();

// MVC
builder.Services.AddControllersWithViews();

// Straturile Application + Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

// ------------------- DATA PROTECTION -------------------
// Cheile semneaza cookie-ul de autentificare si token-ul din fluxul de inregistrare.
// Numele de aplicatie fix le face independente de calea de instalare. Pe mai multe
// instante, sau intr-un container, cheile trebuie sa stea intr-un loc comun si persistent
// (DataProtection:KeysPath); altfel fiecare repornire delogheaza toti utilizatorii.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("CrossTraining");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddSingleton<RegistrationTokenProtector>();
builder.Services.AddScoped<RefreshUserPrincipal>();

// ------------------- PROXY -------------------
// In spatele unui reverse proxy, adresa clientului vine din X-Forwarded-For. Se accepta
// doar de la proxy-urile declarate (ReverseProxy:KnownProxies): altfel oricine ar putea
// trimite headerul si ar ocoli limitarea de incercari pe IP.
var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
if (knownProxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in knownProxies)
            options.KnownProxies.Add(IPAddress.Parse(proxy));
    });
}

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

// Primul in pipeline: tot ce urmeaza (rate limiting, HTTPS, log-uri) trebuie sa vada
// adresa reala a clientului, nu pe cea a proxy-ului.
if (knownProxies.Length > 0)
    app.UseForwardedHeaders();

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

// Un rand de log per cerere (metoda, cale, status, durata), dupa fisierele statice ca sa
// nu le numere si pe ele. Verificarile de sanatate se logheaza la nivelul cel mai jos:
// un orchestrator le apeleaza la cateva secunde.
app.UseSerilogRequestLogging(options =>
{
    // Logger-ul aplicatiei, nu Log.Logger: cel static ramane intentionat neconfigurat
    // (preserveStaticLogger), iar fara asta randurile de cerere s-ar pierde.
    options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
    options.GetLevel = (httpContext, _, exception) =>
        exception != null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
});

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// ------------------- SANATATE -------------------
// /health: procesul raspunde (pentru repornire automata). /health/ready: aplicatia poate
// servi cereri, adica ajunge la baza de date (pentru trimiterea de trafic). Anonime:
// le apeleaza infrastructura, nu un utilizator.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
   .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(InfrastructureServiceCollectionExtensions.ReadinessTag)
}).AllowAnonymous();

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
