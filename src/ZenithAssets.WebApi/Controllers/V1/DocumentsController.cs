using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Features.Documents;

namespace ZenithAssets.WebApi.Controllers.V1;

[Authorize]
public class DocumentsController : ApiControllerBase
{
    /// <summary>
    /// Request access to view confidential property title documents (C of O, Governor's Consent, Survey Plan).
    /// </summary>
    [HttpPost("{documentId}/request-access")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestAccess(Guid documentId, [FromBody] string? reason)
    {
        var result = await Mediator.Send(new RequestDocumentAccessCommand(documentId, reason));
        return HandleResult(result);
    }

    /// <summary>
    /// Approve document access request (Requires Admin role).
    /// </summary>
    [HttpPost("requests/{requestId}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveAccess(Guid requestId, [FromQuery] int validHours = 72)
    {
        var result = await Mediator.Send(new ApproveDocumentAccessCommand(requestId, validHours));
        return HandleResult(result);
    }

    /// <summary>
    /// Generate a time-expiring Pre-Signed URL for an authorized document.
    /// </summary>
    [HttpGet("{documentId}/secure-url")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecureUrl(Guid documentId)
    {
        var result = await Mediator.Send(new GetSecureDocumentUrlQuery(documentId));
        return HandleResult(result);
    }
}

