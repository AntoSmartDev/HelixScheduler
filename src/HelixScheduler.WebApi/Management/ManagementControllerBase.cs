using HelixScheduler.Application.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Management;

public abstract class ManagementControllerBase : ControllerBase
{
    protected ActionResult<T> FromManagementResult<T>(ManagementResult<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        var response = new ManagementFailureResponse(
            result.Errors.Select(error => new ManagementErrorResponse(
                error.Code,
                error.Category.ToString(),
                error.Message,
                error.Target))
            .ToList());

        return StatusCode(MapStatusCode(result.Errors), response);
    }

    private static int MapStatusCode(IReadOnlyList<ManagementError> errors)
    {
        if (errors.Any(error => error.Category == ManagementErrorCategory.NotFound))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errors.Any(error => error.Category == ManagementErrorCategory.Conflict))
        {
            return StatusCodes.Status409Conflict;
        }

        if (errors.Any(error => error.Category == ManagementErrorCategory.InvalidOperation))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
