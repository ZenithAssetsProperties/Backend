using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Application.Features.Reservations;

public record CreateReservationResultDto(
    Guid ReservationId,
    string PaymentReference,
    string CheckoutUrl,
    decimal ReservationFee,
    DateTime ExpiresAtUtc);

public record CreateReservationCommand(
    Guid PropertyId,
    string CallbackUrl,
    string? Notes = null) : IRequest<Result<CreateReservationResultDto>>;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<CreateReservationResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDistributedLockService _lockService;
    private readonly IPaymentGatewayService _paymentGateway;

    // Standard reservation holding fee: ₦2,000,000 or custom deposit
    private static readonly decimal DefaultReservationFee = 2000000m;

    public CreateReservationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDistributedLockService lockService,
        IPaymentGatewayService paymentGateway)
    {
        _context = context;
        _currentUser = currentUser;
        _lockService = lockService;
        _paymentGateway = paymentGateway;
    }

    public async Task<Result<CreateReservationResultDto>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            return Result<CreateReservationResultDto>.Failure("User must be authenticated to reserve a property.");
        }

        var lockKey = $"property:lock:{request.PropertyId}";
        await using var lockHandle = await _lockService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), cancellationToken);

        if (lockHandle == null)
        {
            return Result<CreateReservationResultDto>.Failure("This property is currently being processed for reservation by another buyer. Please try again shortly.");
        }

        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);
        if (property == null)
        {
            return Result<CreateReservationResultDto>.Failure("Property not found.");
        }

        if (property.Status != PropertyStatus.Available)
        {
            return Result<CreateReservationResultDto>.Failure($"This property is no longer available. Current status: {property.Status}.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);
        if (user == null)
        {
            return Result<CreateReservationResultDto>.Failure("User account not found.");
        }

        // Generate high-entropy financial reference
        var paymentRef = $"ZEN-RES-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var reservationFee = Money.Naira(DefaultReservationFee);

        // Transition property state
        property.LockForReservation();

        var reservation = Reservation.Create(
            property.Id,
            user.Id,
            reservationFee,
            paymentRef,
            lockDurationHours: 24,
            notes: request.Notes);

        var transaction = Transaction.Create(
            paymentRef,
            user.Id,
            TransactionType.ReservationFee,
            reservationFee,
            PaymentChannel.Card,
            propertyId: property.Id,
            reservationId: reservation.Id);

        _context.Reservations.Add(reservation);
        _context.Transactions.Add(transaction);

        // Initialize payment gateway (Paystack)
        var gatewayResult = await _paymentGateway.InitializePaymentAsync(
            user.Email,
            reservationFee.Amount,
            reservationFee.Currency,
            paymentRef,
            request.CallbackUrl,
            new Dictionary<string, string>
            {
                ["ReservationId"] = reservation.Id.ToString(),
                ["PropertyId"] = property.Id.ToString(),
                ["UserId"] = user.Id.ToString(),
                ["PropertyTitle"] = property.Title
            },
            cancellationToken);

        if (!gatewayResult.Success)
        {
            // Rollback lock
            property.UnlockToAvailable();
            return Result<CreateReservationResultDto>.Failure($"Payment initialization failed: {gatewayResult.ErrorMessage}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateReservationResultDto>.Success(new CreateReservationResultDto(
            reservation.Id,
            paymentRef,
            gatewayResult.CheckoutUrl,
            reservationFee.Amount,
            reservation.ExpiresAtUtc));
    }
}

public record ReservationSummaryDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    string PropertySlug,
    string Status,
    decimal ReservationFee,
    string FormattedFee,
    string PaymentReference,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? PaidAtUtc);

public record GetMyReservationsQuery : IRequest<Result<List<ReservationSummaryDto>>>;

public class GetMyReservationsQueryHandler : IRequestHandler<GetMyReservationsQuery, Result<List<ReservationSummaryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyReservationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ReservationSummaryDto>>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result<List<ReservationSummaryDto>>.Failure("User must be authenticated.");
        }

        var items = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Property)
            .Where(r => r.UserId == _currentUser.UserId.Value)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReservationSummaryDto(
                r.Id,
                r.PropertyId,
                r.Property.Title,
                r.Property.Slug,
                r.Status.ToString(),
                r.ReservationFee.Amount,
                r.ReservationFee.FormatNaira(),
                r.PaymentReference,
                r.CreatedAtUtc,
                r.ExpiresAtUtc,
                r.PaidAtUtc))
            .ToListAsync(cancellationToken);

        return Result<List<ReservationSummaryDto>>.Success(items);
    }
}
