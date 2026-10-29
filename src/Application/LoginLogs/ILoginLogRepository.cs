using LoginLog = portfolio_users_rest_api.Domain.Entities.LoginLogs;

namespace portfolio_users_rest_api.Application.LoginLogs;

public interface ILoginLogRepository
{
    Task AddLoginLogAsync(LoginLog loginLog, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
