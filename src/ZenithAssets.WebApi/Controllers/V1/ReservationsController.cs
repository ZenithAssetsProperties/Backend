using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Features.Reservations;

namespace ZenithAssets.WebApi.Controllers.V1;

[Authorize]
public class ReservationsController : ApiControllerBase
{
    /// <summary>
    /// Place a 24-hour reservation hold on a property and initiate checkout.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateReservationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all reservations made by the currently authenticated customer.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<ReservationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations()
    {
        var result = await Mediator.Send(new GetMyReservationsQuery());
        return HandleResult(result);
    }
}

