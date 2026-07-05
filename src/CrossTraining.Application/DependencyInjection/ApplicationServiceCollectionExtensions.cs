using CrossTraining.Application.Interfaces;
using CrossTraining.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CrossTraining.Application.DependencyInjection;

/// <summary>
/// Inregistreaza serviciile de business din stratul Application.
/// Repository-urile si PasswordService sunt inregistrate in Infrastructure.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDomainService, DomainService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IClaimService, ClaimService>();

        return services;
    }
}
