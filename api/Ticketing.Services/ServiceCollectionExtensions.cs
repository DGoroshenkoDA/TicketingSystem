using Microsoft.Extensions.DependencyInjection;
using Ticketing.Services.Auth;

namespace Ticketing.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTicketingServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
