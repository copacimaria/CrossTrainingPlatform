using CrossTraining.Application.Interfaces;
using CrossTraining.Domain.Interfaces;
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
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Lipseste connection string-ul 'DefaultConnection'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repository-uri
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IDomainRepository, DomainRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IAppRoleRepository, AppRoleRepository>();
        services.AddScoped<IAppClaimRepository, AppClaimRepository>();

        // Securitate
        services.AddSingleton<IPasswordService, PasswordService>();

        return services;
    }
}
