using Microsoft.Extensions.DependencyInjection;
using Ticketing.Services.Auth;
using Ticketing.Services.Epics;
using Ticketing.Services.Teams;

namespace Ticketing.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTicketingServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IEpicService, EpicService>();
        return services;
    }
}
