using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace UsersApi.Tests;

public class PersistenceErrorTests
{
    [Theory]
    [InlineData(false, "users")]
    [InlineData(true, "users")]
    [InlineData(false, "logs")]
    [InlineData(true, "logs")]
    [InlineData(false, "context")]
    [InlineData(true, "context")]
    public async Task SaveTranslatesEfErrorsAndPreservesTheirCause(bool concurrency, string caller)
    {
        DbUpdateException cause = concurrency ? new DbUpdateConcurrencyException("conflict") : new DbUpdateException("conflict");
        await using var context = CreateContext(cause);

        var error = await Record.ExceptionAsync(() => caller switch
        {
            "users" => new UserRepository(context).SaveChangesAsync(CancellationToken.None),
            "logs" => new LoginLogRepository(context).SaveChangesAsync(CancellationToken.None),
            _ => context.SaveChangesAsync(acceptAllChangesOnSuccess: false)
        });

        Assert.IsType(concurrency ? typeof(ConcurrencyConflictException) : typeof(PersistenceConflictException), error);
        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public async Task SavePreservesCancellation()
    {
        var cause = new OperationCanceledException();
        await using var context = CreateContext(cause);

        var error = await Record.ExceptionAsync(() => context.SaveChangesAsync());

        Assert.Same(cause, error);
    }

    private static UsersDbContext CreateContext(Exception exception) => new(
        new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(new FailingSaveInterceptor(exception))
            .Options);

    private sealed class FailingSaveInterceptor(Exception exception) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) => throw exception;
    }
}
