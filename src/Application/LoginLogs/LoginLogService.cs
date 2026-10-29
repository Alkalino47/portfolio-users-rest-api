using LoginLog = portfolio_users_rest_api.Domain.Entities.LoginLogs;

namespace portfolio_users_rest_api.Application.LoginLogs;

public class LoginLogService(ILoginLogRepository loginLogRepository, TimeProvider timeProvider) : ILoginLogService
{
    private readonly ILoginLogRepository _loginLogRepository = loginLogRepository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<LoginLogDTO> CreateLoginLogAsync(
        LoginLogForPostDTO loginLogForPostDTO,
        CancellationToken cancellationToken)
    {
        var loginLog = new LoginLog
        {
            Username = loginLogForPostDTO.Username,
            LoginTime = ToLocalTime(loginLogForPostDTO.LoginTime!.Value),
            Success = loginLogForPostDTO.Success!.Value,
            IpAddress = loginLogForPostDTO.IpAddress,
            UserAgent = loginLogForPostDTO.UserAgent,
            ErrorMessage = loginLogForPostDTO.ErrorMessage
        };

        await _loginLogRepository.AddLoginLogAsync(loginLog, cancellationToken);
        await _loginLogRepository.SaveChangesAsync(cancellationToken);

        return new LoginLogDTO
        {
            LogId = loginLog.LogId,
            Username = loginLog.Username,
            LoginTime = loginLog.LoginTime,
            Success = loginLog.Success,
            IpAddress = loginLog.IpAddress,
            UserAgent = loginLog.UserAgent,
            ErrorMessage = loginLog.ErrorMessage
        };
    }

    private DateTime ToLocalTime(DateTime loginTime)
    {
        var localTime = loginTime.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(loginTime, _timeProvider.LocalTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(loginTime, _timeProvider.LocalTimeZone),
            _ => loginTime
        };

        // SQL datetime2 stores a wall-clock value without a time-zone offset.
        return DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
    }
}
