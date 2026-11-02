using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenithAssets.Application.Features.Payments;

namespace ZenithAssets.WebApi.Controllers.V1;

public class PaymentsController : ApiControllerBase
{
    /// <summary>
    /// Ingests cryptographically signed payment webhooks from Paystack / payment providers.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["x-paystack-signature"].ToString();

        var result = await Mediator.Send(new ProcessPaymentWebhookCommand(payload, signature));
        return HandleResult(result);
    }

    /// <summary>
    /// Get payment transactions for the currently authenticated customer.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactions()
    {
        var result = await Mediator.Send(new GetMyTransactionsQuery());
        return HandleResult(result);
    }
}

