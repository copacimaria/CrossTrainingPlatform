using System.Diagnostics;
using CrossTraining.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CrossTraining.Application.Common.Behaviors;

/// <summary>
/// Un rand de log per caz de utilizare, cu durata.
///
/// Inainte, niciun serviciu nu injecta ILogger, deci nu exista nicio urma a ce s-a
/// intamplat intr-o cerere. Aici se obtine gratuit pentru toate cazurile de utilizare,
/// fara sa se repete in fiecare handler.
///
/// Se logheaza doar numele cazului de utilizare si motivul unui refuz, niciodata cererea
/// in sine: comenzile de login si de schimbare a parolei contin parole in clar.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            // Verificarea explicita evita evaluarea argumentelor cand nivelul e dezactivat.
            if (_logger.IsEnabled(LogLevel.Information))
            {
                // Un refuz de business (nume deja folosit, rol de sistem) nu este o eroare,
                // dar trebuie sa se vada in log altfel decat o reusita.
                if (response is IUseCaseOutcome { Succeeded: false } refused)
                    _logger.LogInformation("{UseCase} refuzat dupa {ElapsedMs} ms: {Reason}", name, stopwatch.ElapsedMilliseconds, refused.RefusalReason);
                else
                    _logger.LogInformation("{UseCase} a reusit in {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{UseCase} a esuat dupa {ElapsedMs} ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
