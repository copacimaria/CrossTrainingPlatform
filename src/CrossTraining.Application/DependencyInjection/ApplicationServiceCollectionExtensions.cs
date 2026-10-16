using System.Reflection;
using CrossTraining.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrossTraining.Application.DependencyInjection;

/// <summary>
/// Inregistreaza cazurile de utilizare din stratul Application.
///
/// Nu mai exista servicii de inregistrat unul cate unul: handlerele si validatoarele
/// sunt descoperite prin scanarea assembly-ului. Adaugarea unui caz de utilizare nou nu
/// mai cere o linie aici - exact ce lipsea la stratul de servicii, unde fiecare
/// interfata trebuia mapata manual.
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

            // Ordinea conteaza si este intentionata:
            //   1. logarea vede tot, inclusiv esecurile de validare;
            //   2. validarea respinge cererea inainte sa se deschida vreo tranzactie;
            //   3. tranzactia se deschide doar pentru comenzi valide.
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
