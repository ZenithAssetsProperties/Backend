using MediatR;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Common.Models;

namespace ZenithAssets.WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Bad Request",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.Succeeded)
        {
            return Ok(new { success = true });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Bad Request",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }
}

