namespace portfolio_users_rest_api.Application.LoginLogs;

public class LoginLogDTO
{
    public int LogId { get; set; }

    public string Username { get; set; } = null!;

    public DateTime LoginTime { get; set; }

    public bool Success { get; set; }

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public string ErrorMessage { get; set; } = null!;
}
