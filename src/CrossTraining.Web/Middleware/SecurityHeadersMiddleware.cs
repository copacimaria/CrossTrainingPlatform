namespace CrossTraining.Web.Middleware;

/// <summary>
/// Adauga headerele de securitate pe fiecare raspuns.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    /// <summary>
    /// Politica de continut.
    ///
    /// Aplicatia nu are niciun bloc &lt;script&gt; inline - tot JS-ul sta in fisiere din
    /// wwwroot - dar view-urile de administrare folosesc inca 29 de atribute onclick,
    /// pe care CSP le blocheaza fara 'unsafe-inline'. Pana cand acelea devin
    /// addEventListener, script-src ramane relaxat; restul directivelor sunt stricte si
    /// blocheaza deja incarcarea de script, stil sau continut din alte origini.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] = ContentSecurityPolicy;

        // Browserul nu are voie sa ghiceasca alt tip de continut decat cel declarat.
        headers["X-Content-Type-Options"] = "nosniff";

        // Dublura peste frame-ancestors, pentru browsere vechi.
        headers["X-Frame-Options"] = "DENY";

        // Nu trimitem calea completa catre alte origini (poate contine id-uri).
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Aplicatia nu are nevoie de niciuna dintre aceste capabilitati.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";


        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
