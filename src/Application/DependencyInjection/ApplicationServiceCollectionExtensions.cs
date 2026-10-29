using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Application.LoginLogs;
using portfolio_users_rest_api.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace portfolio_users_rest_api.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginLogService, LoginLogService>();

        return services;
    }
}
