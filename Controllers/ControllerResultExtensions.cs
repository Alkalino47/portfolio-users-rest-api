using portfolio_users_rest_api.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace portfolio_users_rest_api.Controllers;

public static class ControllerResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.Succeeded)
        {
            return result.Value is null
                ? controller.NoContent()
                : controller.Ok(result.Value);
        }

        return controller.ToProblemResult(result);
    }

    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.Succeeded)
        {
            return controller.NoContent();
        }

        return controller.ToProblemResult(result);
    }

    private static ObjectResult ToProblemResult(this ControllerBase controller, Result result)
    {
        var statusCode = result.ErrorCode switch
        {
            Errors.NotFound => StatusCodes.Status404NotFound,
            Errors.Validation => StatusCodes.Status400BadRequest,
            Errors.Conflict => StatusCodes.Status409Conflict,
            Errors.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(result.ErrorCode),
            Detail = result.ErrorMessage,
            Instance = controller.HttpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        problemDetails.Extensions["errorCode"] = result.ErrorCode;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static string GetTitle(string? errorCode) => errorCode switch
    {
        Errors.NotFound => "Resource not found",
        Errors.Validation => "Validation error",
        Errors.Conflict => "Conflict",
        Errors.Unauthorized => "Unauthorized",
        _ => "Request failed"
    };
}
