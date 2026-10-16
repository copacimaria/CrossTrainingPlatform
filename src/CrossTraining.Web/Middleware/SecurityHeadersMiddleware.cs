namespace CrossTraining.Web.Middleware;

/// <summary>
/// Adauga headerele de securitate pe fiecare raspuns.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    /// <summary>
    /// Politica de continut.
    ///
    /// script-src 'self', fara 'unsafe-inline': browserul ruleaza doar fisierele din
    /// wwwroot. Un script injectat in pagina (un bloc &lt;script&gt;, un onclick=..., un
    /// href="javascript:...") nu se executa, chiar daca ar scapa de codificarea Razor.
    /// View-urile nu au handler-e inline: comportamentul vine din atribute data-*, legate
    /// de admin.js. Testul InlineScriptScanner verifica asta pe paginile randate.
    ///
    /// style-src pastreaza 'unsafe-inline': view-urile si Bootstrap folosesc atribute
    /// style, iar un stil injectat nu poate rula cod.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
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
