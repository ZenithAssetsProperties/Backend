using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Features.Analytics;
using ZenithAssets.Application.Features.Inspections;
using ZenithAssets.Application.Features.Leads;

namespace ZenithAssets.WebApi.Controllers.V1;

public class InspectionsController : ApiControllerBase
{
    /// <summary>
    /// Schedule a physical or live 360 virtual tour inspection for a property.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BookInspection([FromBody] BookInspectionCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

public class LeadsController : ApiControllerBase
{
    /// <summary>
    /// Capture inbound lead from Website, WhatsApp, Instagram, or direct inquiry.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CaptureLead([FromBody] CaptureLeadCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ApiControllerBase
{
    /// <summary>
    /// Real-time executive KPIs (portfolio value, total revenue, leads, properties).
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ExecutiveAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics()
    {
        var result = await Mediator.Send(new GetExecutiveAnalyticsQuery());
        return HandleResult(result);
    }
}

