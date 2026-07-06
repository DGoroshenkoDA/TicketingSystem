using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.Api.Common;

// Consistent response envelope:
//   success: { "success": true, "data": ... }
//   error:   { "success": false, "code": "...", "detail": "..." }
public static class ApiResults
{
    public static IActionResult Success(object? data, int statusCode = StatusCodes.Status200OK)
        => new ObjectResult(new { success = true, data }) { StatusCode = statusCode };

    public static IActionResult Failure(Error error)
        => new ObjectResult(new { success = false, code = error.Code, detail = error.Description })
        {
            StatusCode = StatusFor(error.Type)
        };

    public static IActionResult ValidationFailure(string detail)
        => new ObjectResult(new { success = false, code = "Validation", detail })
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
