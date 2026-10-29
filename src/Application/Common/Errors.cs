namespace portfolio_users_rest_api.Application.Common;

public static class Errors
{
    public const string NotFound = "not_found";
    public const string Validation = "validation_error";
    public const string Conflict = "conflict";
    public const string Unauthorized = "unauthorized";
}

public class PersistenceConflictException(string message, Exception innerException) : Exception(message, innerException);

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : PersistenceConflictException(message, innerException);
