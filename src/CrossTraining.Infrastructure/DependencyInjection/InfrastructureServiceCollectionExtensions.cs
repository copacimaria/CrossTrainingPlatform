using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Infrastructure.Persistence;
using CrossTraining.Infrastructure.Repositories;
using CrossTraining.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrossTraining.Infrastructure.DependencyInjection;

/// <summary>
/// Inregistreaza DbContext-ul, repository-urile si serviciile de securitate din Infrastructure.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Eticheta verificarilor care spun daca aplicatia poate servi cereri (/health/ready).</summary>
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Connection string-ul nu este in appsettings.json versionat. Local vine din profilul
        // CrossTraining.Web, in rest din variabile de mediu sau dintr-un secret store.
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Lipseste connection string-ul 'DefaultConnection'. Local, porneste aplicatia cu " +
                "profilul CrossTraining.Web din launchSettings.json; in alte medii, seteaza variabila " +
                "de mediu ConnectionStrings__DefaultConnection.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repository-uri
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IDomainRepository, DomainRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IAppRoleRepository, AppRoleRepository>();
        services.AddScoped<IAppClaimRepository, AppClaimRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Securitate
        services.AddSingleton<IPasswordService, PasswordService>();

        // Baza de date este singura dependinta fara de care aplicatia nu poate servi nimic,
        // deci intra in verificarea de disponibilitate. Inregistrata aici, nu in Web, ca
        // DbContext-ul sa ramana cunoscut doar de Infrastructure.
        services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("database", tags: [ReadinessTag]);

        return services;
    }
}
