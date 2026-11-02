using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Domain.Entities;

public sealed class Transaction : AuditableEntity<Guid>
{
    public string Reference { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public Guid? PropertyId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentChannel Channel { get; private set; } = PaymentChannel.Card;
    public string? GatewayReference { get; private set; }
    public string? GatewayResponseJson { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string? ReceiptPdfKey { get; private set; }

    public User User { get; private set; } = null!;
    public Property? Property { get; private set; }
    public Reservation? Reservation { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        string reference,
        Guid userId,
        TransactionType transactionType,
        Money amount,
        PaymentChannel channel,
        Guid? propertyId = null,
        Guid? reservationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            Reference = reference.Trim(),
            UserId = userId,
            PropertyId = propertyId,
            ReservationId = reservationId,
            TransactionType = transactionType,
            Amount = amount,
            Channel = channel,
            Status = PaymentStatus.Pending
        };
    }

    public void MarkSuccess(string gatewayReference, DateTime paidAtUtc, string? gatewayResponseJson = null)
    {
        Status = PaymentStatus.Success;
        GatewayReference = gatewayReference;
        PaidAtUtc = paidAtUtc;
        GatewayResponseJson = gatewayResponseJson;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string? failureReasonJson = null)
    {
        Status = PaymentStatus.Failed;
        GatewayResponseJson = failureReasonJson;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void AttachReceipt(string receiptKey)
    {
        ReceiptPdfKey = receiptKey;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

public sealed class PaymentPlan : AuditableEntity<Guid>
{
    public Guid PropertyId { get; private set; }
    public Guid UserId { get; private set; }
    public Money TotalPrice { get; private set; } = Money.Zero();
    public Money InitialDeposit { get; private set; } = Money.Zero();
    public Money BalanceRemaining { get; private set; } = Money.Zero();
    public int TenureMonths { get; private set; }
    public Money MonthlyInstallmentAmount { get; private set; } = Money.Zero();
    public DateTime StartDateUtc { get; private set; }
    public DateTime NextDueDateUtc { get; private set; }
    public bool IsCompleted { get; private set; }

    public Property Property { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private PaymentPlan() { }

    public static PaymentPlan Create(
        Guid propertyId,
        Guid userId,
        Money totalPrice,
        Money initialDeposit,
        int tenureMonths)
    {
        if (tenureMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenureMonths), "Tenure must be at least 1 month.");

        var balance = totalPrice.Subtract(initialDeposit);
        var monthly = new Money(decimal.Round(balance.Amount / tenureMonths, 2), balance.Currency);

        return new PaymentPlan
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            TotalPrice = totalPrice,
            InitialDeposit = initialDeposit,
            BalanceRemaining = balance,
            TenureMonths = tenureMonths,
            MonthlyInstallmentAmount = monthly,
            StartDateUtc = DateTime.UtcNow,
            NextDueDateUtc = DateTime.UtcNow.AddMonths(1),
            IsCompleted = balance.Amount <= 0
        };
    }

    public void RecordInstallmentPayment(Money payment)
    {
        BalanceRemaining = BalanceRemaining.Subtract(payment);
        NextDueDateUtc = NextDueDateUtc.AddMonths(1);
        if (BalanceRemaining.Amount <= 0)
        {
            IsCompleted = true;
        }
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

