using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Domain.Entities;

public sealed class Reservation : AuditableEntity<Guid>
{
    public Guid PropertyId { get; private set; }
    public Guid UserId { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.PendingPayment;
    public Money ReservationFee { get; private set; } = Money.Zero();
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string PaymentReference { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public Property Property { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public ICollection<Transaction> Transactions { get; private set; } = [];

    private Reservation() { }

    public static Reservation Create(
        Guid propertyId,
        Guid userId,
        Money reservationFee,
        string paymentReference,
        int lockDurationHours = 24,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);

        return new Reservation
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            ReservationFee = reservationFee,
            Status = ReservationStatus.PendingPayment,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(lockDurationHours),
            PaymentReference = paymentReference.Trim(),
            Notes = notes
        };
    }

    public void ConfirmPayment(DateTime paidAtUtc)
    {
        Status = ReservationStatus.Confirmed;
        PaidAtUtc = paidAtUtc;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status == ReservationStatus.PendingPayment)
        {
            Status = ReservationStatus.Expired;
            LastModifiedAtUtc = DateTime.UtcNow;
        }
    }

    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public bool IsExpired() => Status == ReservationStatus.PendingPayment && DateTime.UtcNow > ExpiresAtUtc;
}

