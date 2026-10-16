using CrossTraining.Application.Features.Authentication;
using CrossTraining.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CrossTraining.Web.Security;

/// <summary>
/// Reciteste rolul si permisiunile din baza de date la un interval scurt si rescrie
/// cookie-ul.
///
/// Fara asta, claim-urile raman inghetate la momentul login-ului: o permisiune revocata
/// sau un cont dezactivat ar continua sa functioneze pana la expirarea cookie-ului, adica
/// pana la o ora. Cu revalidare, fereastra scade la intervalul configurat (implicit 5 min).
///
/// Compromisul este o interogare in plus la fiecare interval, per sesiune activa - nu la
/// fiecare cerere.
/// </summary>
public sealed class RefreshUserPrincipal
{
    /// <summary>Cheia din configuratie care controleaza intervalul, in secunde.</summary>
    public const string IntervalSettingKey = "Security:PrincipalRefreshSeconds";

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    private readonly ISender _sender;
    private readonly TimeProvider _clock;
    private readonly ILogger<RefreshUserPrincipal> _logger;
    private readonly TimeSpan _refreshInterval;

    public RefreshUserPrincipal(
        ISender sender,
        TimeProvider clock,
        IConfiguration configuration,
        ILogger<RefreshUserPrincipal> logger)
    {
        _sender = sender;
        _clock = clock;
        _logger = logger;

        // Configurabil ca sa poata fi testat: cu intervalul implicit de cinci minute, un
        // test ar trebui sa astepte cinci minute ca sa observe reimprospatarea.
        _refreshInterval = int.TryParse(configuration[IntervalSettingKey], out var seconds) && seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : DefaultInterval;
    }

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
            return;

        var userId = principal.GetUserId();
        if (userId == null)
        {
            Reject(context, "identitate fara id de utilizator");
            return;
        }

        var now = _clock.GetUtcNow();
        var refreshedAt = UserPrincipalFactory.ReadRefreshedAt(principal);

        // Sesiunile emise inainte de introducerea claim-ului nu au marcaj de timp:
        // le tratam ca expirate, deci se reimprospateaza la prima cerere.
        if (refreshedAt != null && now - refreshedAt.Value < _refreshInterval)
            return;

        var securityContext = await _sender.Send(new GetUserSecurityContextQuery(userId.Value));
        if (securityContext == null)
        {
            // Contul a fost sters sau dezactivat intre timp.
            Reject(context, "cont inexistent sau dezactivat");
            return;
        }

        context.ReplacePrincipal(UserPrincipalFactory.Create(securityContext, now));
        context.ShouldRenew = true;
    }

    private void Reject(CookieValidatePrincipalContext context, string reason)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Sesiune respinsa la revalidare: {Reason}", reason);

        context.RejectPrincipal();
    }
}
