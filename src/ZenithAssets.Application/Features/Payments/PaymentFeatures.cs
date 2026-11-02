using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Features.Payments;

public record ProcessPaymentWebhookCommand(string Payload, string Signature) : IRequest<Result>;

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly INotificationService _notifications;

    public ProcessPaymentWebhookCommandHandler(
        IApplicationDbContext context,
        IPaymentGatewayService paymentGateway,
        INotificationService notifications)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _notifications = notifications;
    }

    public async Task<Result> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify cryptographic HMAC-SHA512 signature from Paystack
        if (!_paymentGateway.VerifyWebhookSignature(request.Payload, request.Signature))
        {
            return Result.Failure("Invalid webhook cryptographic signature.");
        }

        // 2. Parse payload
        using var jsonDoc = JsonDocument.Parse(request.Payload);
        var root = jsonDoc.RootElement;

        var eventName = root.GetProperty("event").GetString();
        if (eventName != "charge.success")
        {
            // Acknowledge non-charge events cleanly
            return Result.Success();
        }

        var data = root.GetProperty("data");
        var reference = data.GetProperty("reference").GetString();
        var gatewayRef = data.GetProperty("id").ToString();
        var status = data.GetProperty("status").GetString();

        if (string.IsNullOrWhiteSpace(reference) || status != "success")
        {
            return Result.Failure("Malformed webhook data or unapproved charge.");
        }

        // 3. Find transaction (Idempotency check)
        var transaction = await _context.Transactions
            .Include(t => t.Reservation)
            .Include(t => t.Property)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Reference == reference, cancellationToken);

        if (transaction == null)
        {
            return Result.Failure($"Transaction with reference {reference} not found in system.");
        }

        if (transaction.Status == PaymentStatus.Success)
        {
            // Already processed idempotently
            return Result.Success();
        }

        // 4. Update transaction
        transaction.MarkSuccess(gatewayRef, DateTime.UtcNow, request.Payload);

        // 5. Update linked reservation and property
        if (transaction.Reservation != null)
        {
            transaction.Reservation.ConfirmPayment(DateTime.UtcNow);
        }

        if (transaction.Property != null)
        {
            transaction.Property.ConfirmReservation();
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Multi-channel notifications to buyer
        var buyer = transaction.User;
        var propertyTitle = transaction.Property?.Title ?? "Property";
        var amountFormatted = transaction.Amount.FormatNaira();

        var emailSubject = $"Payment Confirmation — {propertyTitle} Reserved Successfully";
        var emailBody = $@"
            <h2>Congratulations {buyer.FullName},</h2>
            <p>Your payment of <strong>{amountFormatted}</strong> for <strong>{propertyTitle}</strong> has been confirmed.</p>
            <p><strong>Transaction Reference:</strong> {reference}</p>
            <p>Your property reservation is now officially confirmed. A Zenith Asset Group consultant has been assigned to your transaction.</p>
            <p>You can access your documentation and track your transaction progress directly from your Customer Dashboard.</p>
        ";

        await _notifications.SendEmailAsync(buyer.Email, emailSubject, emailBody, cancellationToken);

        var smsMsg = $"Zenith Asset: Payment of {amountFormatted} received for {propertyTitle}. Ref: {reference}. Check your dashboard for details.";
        await _notifications.SendSmsAsync(buyer.PhoneNumber, smsMsg, cancellationToken);

        var whatsAppMsg = $"Hello *{buyer.FullName}*,\n\nWe have received your payment of *{amountFormatted}* for *{propertyTitle}*.\nTransaction Reference: `{reference}`.\n\nYour reservation is confirmed! You can view your digital receipt in your dashboard: https://app.zenithassets.ng";
        await _notifications.SendWhatsAppMessageAsync(buyer.PhoneNumber, whatsAppMsg, cancellationToken);

        return Result.Success();
    }
}

public record TransactionDto(
    Guid Id,
    string Reference,
    string TransactionType,
    decimal Amount,
    string FormattedAmount,
    string Status,
    string Channel,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    string? PropertyTitle);

public record GetMyTransactionsQuery : IRequest<Result<List<TransactionDto>>>;

public class GetMyTransactionsQueryHandler : IRequestHandler<GetMyTransactionsQuery, Result<List<TransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyTransactionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<TransactionDto>>> Handle(GetMyTransactionsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result<List<TransactionDto>>.Failure("User must be authenticated.");
        }

        var items = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Property)
            .Where(t => t.UserId == _currentUser.UserId.Value)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TransactionDto(
                t.Id,
                t.Reference,
                t.TransactionType.ToString(),
                t.Amount.Amount,
                t.Amount.FormatNaira(),
                t.Status.ToString(),
                t.Channel.ToString(),
                t.CreatedAtUtc,
                t.PaidAtUtc,
                t.Property != null ? t.Property.Title : null))
            .ToListAsync(cancellationToken);

        return Result<List<TransactionDto>>.Success(items);
    }
}
