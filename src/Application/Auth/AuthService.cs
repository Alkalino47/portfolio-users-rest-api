using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Application.LoginLogs;
using portfolio_users_rest_api.Application.Users;
using Microsoft.Extensions.Logging;

namespace portfolio_users_rest_api.Application.Auth;

public class AuthService(
    IUserRepository userRepository,
    IUserPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ILoginLogService loginLogService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUserPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ILoginLogService _loginLogService = loginLogService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<Result<AuthLoginResponseDTO>> LoginAsync(
        AuthLoginRequestDTO loginRequest,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var userName = loginRequest.UserName.Trim();
        var loginTime = _timeProvider.GetLocalNow().DateTime;
        Result<AuthLoginResponseDTO>? loginResult = null;
        var errorMessage = "An unexpected error occurred during login.";

        try
        {
            var user = await _userRepository.GetUserByUserNameAsync(userName, cancellationToken);
            if (user == null)
            {
                return loginResult = Result<AuthLoginResponseDTO>.Failure(Errors.Unauthorized, InvalidCredentialsMessage);
            }

            if (!_passwordHasher.VerifyPassword(user, loginRequest.UserPass))
            {
                return loginResult = Result<AuthLoginResponseDTO>.Failure(Errors.Unauthorized, InvalidCredentialsMessage);
            }

            if (!user.IsActive)
            {
                return loginResult = Result<AuthLoginResponseDTO>.Failure(Errors.Unauthorized, "User is inactive.");
            }

            return loginResult = Result<AuthLoginResponseDTO>.Success(new AuthLoginResponseDTO
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserRole = user.UserRole?.RoleName ?? string.Empty,
                IsActive = user.IsActive,
                Token = _jwtTokenService.CreateAccessToken(user)
            });
        }
        catch (OperationCanceledException)
        {
            errorMessage = "Login request was canceled.";
            throw;
        }
        finally
        {
            var loginLog = new LoginLogForPostDTO
            {
                Username = Truncate(userName, 100),
                LoginTime = loginTime,
                Success = loginResult?.Succeeded ?? false,
                IpAddress = Truncate(string.IsNullOrWhiteSpace(ipAddress) ? "Unknown" : ipAddress, 45),
                UserAgent = Truncate(string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent, 1024),
                ErrorMessage = loginResult is null ? errorMessage : loginResult.ErrorMessage ?? string.Empty
            };

            try
            {
                // Use a separate timeout so a canceled request still gets one logging attempt.
                using var loggingTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _loginLogService.CreateLoginLogAsync(loginLog, loggingTimeout.Token);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to save the login attempt log.");
            }
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
