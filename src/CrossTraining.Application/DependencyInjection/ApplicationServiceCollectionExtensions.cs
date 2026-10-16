using System.Reflection;
using CrossTraining.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrossTraining.Application.DependencyInjection;

/// <summary>
/// Inregistreaza cazurile de utilizare din stratul Application.
///
/// Nu exista servicii de inregistrat unul cate unul: handlerele sunt descoperite prin
/// scanarea assembly-ului. Adaugarea unui caz de utilizare nou nu cere o linie aici.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Serviciile citesc timpul prin TimeProvider, nu prin DateTime.Now, ca sa poata
        // fi testate cu un ceas controlat. TryAdd, ca sa poata fi inlocuit din teste.
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);

            // Ordinea conteaza: logarea vede tot, inclusiv refuzurile si exceptiile din
            // tranzactie. Nu mai exista un pas separat de validare: invariantii stau in
            // entitati, iar formularele din Web valideaza ce vede utilizatorul (ADR-0010).
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        return services;
    }
}
