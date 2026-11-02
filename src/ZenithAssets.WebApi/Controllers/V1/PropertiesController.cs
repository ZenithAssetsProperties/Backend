using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Application.Features.Properties;

namespace ZenithAssets.WebApi.Controllers.V1;

public class PropertiesController : ApiControllerBase
{
    /// <summary>
    /// Search and filter properties with faceted queries (location, price, bedrooms, status).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedList<PropertySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchPropertiesQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve full details of a property by its URL slug.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PropertyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var result = await Mediator.Send(new GetPropertyBySlugQuery(slug));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new property listing (Requires Admin role).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePropertyCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}

