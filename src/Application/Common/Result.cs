namespace portfolio_users_rest_api.Application.Common;

public class Result
{
    protected Result(bool succeeded, string? errorCode, string? errorMessage)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}

public class Result<T> : Result
{
    private Result(T? value, bool succeeded, string? errorCode, string? errorMessage)
        : base(succeeded, errorCode, errorMessage)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, null, null);

    public static new Result<T> Failure(string errorCode, string errorMessage) => new(default, false, errorCode, errorMessage);
}
