using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Application.LoginLogs;
using portfolio_users_rest_api.Application.Users;
using portfolio_users_rest_api.Domain.Entities;
using portfolio_users_rest_api.Infrastructure.Auth;
using portfolio_users_rest_api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace portfolio_users_rest_api.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var usersConnectionString = configuration.GetConnectionString("UserDBConn");
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddDbContext<UsersDbContext>(options => options.UseSqlServer(usersConnectionString));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IUserPasswordHasher, IdentityUserPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoginLogRepository, LoginLogRepository>();

        return services;
    }
}
