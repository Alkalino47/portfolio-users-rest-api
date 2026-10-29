using portfolio_users_rest_api.Application.LoginLogs;
using LoginLog = portfolio_users_rest_api.Domain.Entities.LoginLogs;

namespace portfolio_users_rest_api.Infrastructure.Persistence;

public class LoginLogRepository(UsersDbContext context) : ILoginLogRepository
{
    private readonly UsersDbContext _context = context;

    public async Task AddLoginLogAsync(LoginLog loginLog, CancellationToken cancellationToken)
    {
        await _context.LoginLogs.AddAsync(loginLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
