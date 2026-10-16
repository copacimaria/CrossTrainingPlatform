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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Connection string-ul nu mai este in appsettings.json versionat. In dezvoltare vine
        // din user-secrets, in rest din variabile de mediu sau secret store.
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Lipseste connection string-ul 'DefaultConnection'. In dezvoltare, ruleaza: " +
                "dotnet user-secrets --project src/CrossTraining.Web set " +
                "\"ConnectionStrings:DefaultConnection\" \"<connection string>\"");

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

        return services;
    }
}
