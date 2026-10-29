using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using portfolio_users_rest_api.Application.Auth;
using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Application.LoginLogs;
using portfolio_users_rest_api.Application.Users;
using portfolio_users_rest_api.Domain.Entities;
using portfolio_users_rest_api.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace UsersApi.Tests;

public class ApiWorkflowTests
{
    [Fact]
    public async Task LoginAndAllUserOperationsPreserveTheirHttpContracts()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var seedLogin = await Login(client, "seed-user", "SeedPassword123!");
        Assert.Equal(HttpStatusCode.OK, seedLogin.StatusCode);
        await Authorize(client, seedLogin);

        var create = await client.PostAsJsonAsync("/api/v1/user", new
        {
            userName = "created-user",
            userPass = "CreatedPassword123!",
            userRoleId = 2,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<UserDTO>();
        Assert.NotNull(created);
        Assert.EndsWith($"/api/v1/user/{created.UserId}", create.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userPass", await create.Content.ReadAsStringAsync());
        Assert.NotEqual("CreatedPassword123!", factory.Store.Users.Single(u => u.UserId == created.UserId).UserPass);

        var login = await Login(client, "created-user", "CreatedPassword123!");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        await Authorize(client, login);
        Assert.Equal(2, factory.Store.Logs.Count);
        Assert.All(factory.Store.Logs, log => Assert.True(log.Success));

        var list = await client.GetFromJsonAsync<PaginatedResult<UserDTO>>("/api/v1/user?search=created-user&pageSize=5");
        Assert.NotNull(list);
        Assert.Equal(created.UserId, Assert.Single(list.Items).UserId);
        Assert.Equal(1, list.Pagination.TotalCount);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/user/{created.UserId}")).StatusCode);

        var update = await client.PutAsJsonAsync($"/api/v1/user/{created.UserId}", new
        {
            userId = created.UserId,
            userName = "updated-user",
            userPass = "UpdatedPassword123!",
            userRoleId = 1,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("updated-user", (await update.Content.ReadFromJsonAsync<UserDTO>())!.UserName);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, "updated-user", "UpdatedPassword123!")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "updated-user", "CreatedPassword123!")).StatusCode);

        var patch = await client.PatchAsJsonAsync($"/api/v1/user/{created.UserId}/active", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.False((await patch.Content.ReadFromJsonAsync<UserDTO>())!.IsActive);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "updated-user", "UpdatedPassword123!")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/user/{created.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/user/{created.UserId}")).StatusCode);
    }

    [Theory]
    [InlineData("unknown", "SeedPassword123!", false, "Invalid username or password.")]
    [InlineData("seed-user", "incorrect", false, "Invalid username or password.")]
    [InlineData("seed-user", "SeedPassword123!", true, "User is inactive.")]
    public async Task RejectedLoginReturnsNoTokenAndLogsOnce(string userName, string password, bool inactive, string reason)
    {
        using var factory = new ApiFactory();
        factory.Store.Users[0].IsActive = !inactive;
        using var client = factory.CreateClient();

        var response = await Login(client, userName, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
        var log = Assert.Single(factory.Store.Logs);
        Assert.Equal(1, factory.Store.LogAttempts);
        Assert.False(log.Success);
        Assert.Equal(reason, log.ErrorMessage);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("expired")]
    public async Task ProtectedOperationsRejectMissingInvalidOrExpiredTokens(string tokenKind)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        if (tokenKind != "missing")
        {
            var jwtOptions = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
            var token = tokenKind == "expired"
                ? new JwtTokenService(Options.Create(new JwtOptions
                {
                    Issuer = jwtOptions.Issuer,
                    Audiences = jwtOptions.Audiences,
                    SigningKey = jwtOptions.SigningKey,
                    ExpiresMinutes = -10
                })).CreateAccessToken(factory.Store.Users[0]).AccessToken
                : "invalid-token";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/user")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/user", new { })).StatusCode);
    }

    [Fact]
    public async Task InvalidLoginBodyIsRejectedBeforeAuditing()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "seed-user" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Store.LogAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuditFailurePreservesTheLoginOutcome(bool validPassword)
    {
        using var factory = new ApiFactory();
        factory.Store.FailAudit = true;
        using var client = factory.CreateClient();

        var response = await Login(client, "seed-user", validPassword ? "SeedPassword123!" : "incorrect");

        Assert.Equal(validPassword ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, factory.Store.LogAttempts);
    }

    [Fact]
    public async Task CanceledLoginStillMakesOneIndependentAuditAttempt()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAuthService>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LoginAsync(
            new AuthLoginRequestDTO { UserName = "seed-user", UserPass = "SeedPassword123!" },
            null, null, cancellation.Token));

        Assert.Equal(1, factory.Store.LogAttempts);
        Assert.Equal("Login request was canceled.", Assert.Single(factory.Store.Logs).ErrorMessage);
    }

    [Theory]
    [InlineData(false, "Database update failed")]
    [InlineData(true, "Concurrency conflict")]
    public async Task PersistenceConflictsKeepTheirProblemDetails(bool concurrency, string title)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await Authorize(client, await Login(client, "seed-user", "SeedPassword123!"));
        factory.Store.SaveFailure = concurrency
            ? new ConcurrencyConflictException("conflict", new Exception("database detail"))
            : new PersistenceConflictException("conflict", new Exception("database detail"));

        var response = await client.DeleteAsync("/api/v1/user/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("database detail", await response.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string userName, string userPass) =>
        client.PostAsJsonAsync("/api/auth/login", new { userName, userPass });

    private static async Task Authorize(HttpClient client, HttpResponseMessage login)
    {
        var body = await login.Content.ReadFromJsonAsync<AuthLoginResponseDTO>();
        Assert.NotNull(body);
        Assert.Equal("Bearer", body.Token.TokenType);
        Assert.True(body.Token.ExpiresAtUtc > DateTime.UtcNow);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token.AccessToken);
    }
}

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    internal TestStore Store { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ILoginLogRepository>();
            services.AddSingleton<IUserRepository>(Store);
            services.AddSingleton<ILoginLogRepository>(Store);
        });
    }
}

// The HTTP tests exercise real controllers, services, hashing, JWTs and middleware;
// only repository storage is replaced, so they do not require SQL Server.
internal sealed class TestStore : IUserRepository, ILoginLogRepository
{
    internal List<User> Users { get; } = [];
    internal List<LoginLogs> Logs { get; } = [];
    internal int LogAttempts { get; private set; }
    internal bool FailAudit { get; set; }
    internal Exception? SaveFailure { get; set; }

    internal TestStore()
    {
        var user = new User { UserId = 1, UserName = "seed-user", UserRoleId = 1, IsActive = true };
        user.UserPass = new PasswordHasher<User>().HashPassword(user, "SeedPassword123!");
        Users.Add(user);
    }

    public Task<IReadOnlyList<User>> GetUsersAsync(UserPaginationQuery query, int skip, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(Filter(query).Skip(skip).Take(take).ToList());
    public Task<int> CountUsersAsync(UserPaginationQuery query, CancellationToken cancellationToken) => Task.FromResult(Filter(query).Count());
    public Task<User?> GetUserAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Users.SingleOrDefault(u => u.UserId == id));
    public Task<User?> GetUserForUpdateAsync(int id, CancellationToken cancellationToken) => GetUserAsync(id, cancellationToken);
    public Task<bool> UserExistsAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Users.Any(u => u.UserId == id && u.IsActive));

    public Task<User?> GetUserByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Users.SingleOrDefault(u => u.UserName == userName));
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        user.UserId = Users.Max(u => u.UserId) + 1;
        user.UserRole = new UserRole { UserRoleId = user.UserRoleId, RoleName = user.UserRoleId == 1 ? "Admin" : "User" };
        Users.Add(user);
        return Task.CompletedTask;
    }

    public void DeleteUser(User user) => Users.Remove(user);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => SaveFailure is null ? Task.CompletedTask : Task.FromException(SaveFailure);

    public Task AddLoginLogAsync(LoginLogs loginLog, CancellationToken cancellationToken)
    {
        LogAttempts++;
        cancellationToken.ThrowIfCancellationRequested();
        if (FailAudit) throw new InvalidOperationException("audit unavailable");
        loginLog.LogId = Logs.Count + 1;
        Logs.Add(loginLog);
        return Task.CompletedTask;
    }

    private IEnumerable<User> Filter(UserPaginationQuery query) => Users
        .Where(u => query.IsActive is null || u.IsActive == query.IsActive)
        .Where(u => string.IsNullOrWhiteSpace(query.Search) || u.UserName.Contains(query.Search.Trim()));
}
