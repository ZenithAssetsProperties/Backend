using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Features.Inspections;

public record BookInspectionCommand(
    Guid PropertyId,
    string ClientName,
    string ClientEmail,
    string ClientPhone,
    InspectionType InspectionType,
    DateTime PreferredDateUtc,
    string PreferredTimeSlot,
    int NumberOfVisitors = 1,
    string? SpecialRequest = null) : IRequest<Result<Guid>>;

public class BookInspectionCommandValidator : AbstractValidator<BookInspectionCommand>
{
    public BookInspectionCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ClientEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ClientPhone).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PreferredDateUtc).GreaterThan(DateTime.UtcNow.Date);
        RuleFor(x => x.PreferredTimeSlot).NotEmpty();
    }
}

public class BookInspectionCommandHandler : IRequestHandler<BookInspectionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public BookInspectionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(BookInspectionCommand request, CancellationToken cancellationToken)
    {
        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId, cancellationToken);
        if (property == null)
        {
            return Result<Guid>.Failure("Property not found.");
        }

        var inspection = Inspection.Create(
            property.Id,
            request.ClientName,
            request.ClientEmail,
            request.ClientPhone,
            request.InspectionType,
            request.PreferredDateUtc,
            request.PreferredTimeSlot,
            request.NumberOfVisitors,
            request.SpecialRequest,
            userId: _currentUser.UserId);

        // Also create or link a CRM Lead automatically
        var lead = Lead.Create(
            request.ClientName,
            request.ClientEmail,
            request.ClientPhone,
            LeadSource.Website,
            interestedPropertyId: property.Id,
            message: $"Inspection booked ({request.InspectionType}) on {request.PreferredDateUtc:yyyy-MM-dd} at {request.PreferredTimeSlot}");

        _context.Inspections.Add(inspection);
        _context.Leads.Add(lead);

        await _context.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        var subject = $"Inspection Request Received — {property.Title}";
        var body = $@"
            <h3>Dear {request.ClientName},</h3>
            <p>Your inspection request for <strong>{property.Title}</strong> has been received.</p>
            <p><strong>Inspection Mode:</strong> {request.InspectionType}</p>
            <p><strong>Preferred Date:</strong> {request.PreferredDateUtc:dddd, MMMM dd, yyyy}</p>
            <p><strong>Time Slot:</strong> {request.PreferredTimeSlot}</p>
            <p>A Zenith Asset Group property consultant will contact you shortly to confirm your appointment.</p>
        ";
        await _notifications.SendEmailAsync(request.ClientEmail, subject, body, cancellationToken);

        return Result<Guid>.Success(inspection.Id);
    }
}

