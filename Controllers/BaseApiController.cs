using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ValidationBadRequest(ValidationResult validationResult, string message = "Validation failed")
    {
        return BadRequest(new
        {
            message,
            errors = validationResult.ToDictionary()
        });
    }

    protected string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
