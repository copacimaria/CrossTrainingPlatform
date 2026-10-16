using FluentValidation;
using MediatR;

namespace CrossTraining.Application.Common.Behaviors;

/// <summary>
/// Ruleaza validatoarele inregistrate pentru cerere, inainte de handler.
///
/// Validarea de aici apara cazul de utilizare indiferent de apelant: acopera invarianti
/// (un nume nu poate fi gol, un id trebuie sa fie pozitiv), nu mesaje pentru utilizator -
/// acelea stau pe modelele de formular din stratul Web, care stiu ce ecran se afiseaza.
/// Regulile care cer acces la date (emailul e deja luat?) raman in handler si se intorc
/// ca rezultat, nu ca exceptie: sunt raspunsuri de business, nu date invalide.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
