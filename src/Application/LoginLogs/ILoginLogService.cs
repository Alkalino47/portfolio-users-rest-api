namespace portfolio_users_rest_api.Application.LoginLogs;

public interface ILoginLogService
{
    Task<LoginLogDTO> CreateLoginLogAsync(LoginLogForPostDTO loginLogForPostDTO, CancellationToken cancellationToken);
}
