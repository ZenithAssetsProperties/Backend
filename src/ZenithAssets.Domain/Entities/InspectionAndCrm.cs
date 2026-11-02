using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Domain.Entities;

public sealed class Inspection : AuditableEntity<Guid>
{
    public Guid PropertyId { get; private set; }
    public Guid? UserId { get; private set; }
    public string ClientName { get; private set; } = string.Empty;
    public string ClientEmail { get; private set; } = string.Empty;
    public string ClientPhone { get; private set; } = string.Empty;
    public InspectionType InspectionType { get; private set; }
    public InspectionStatus Status { get; private set; } = InspectionStatus.Requested;
    public DateTime PreferredDateUtc { get; private set; }
    public string PreferredTimeSlot { get; private set; } = "10:00 AM";
    public int NumberOfVisitors { get; private set; } = 1;
    public string? SpecialRequest { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public string? MeetingUrl { get; private set; } // For virtual live tour
    public string? AgentNotes { get; private set; }

    public Property Property { get; private set; } = null!;
    public User? User { get; private set; }
    public User? AssignedAgent { get; private set; }

    private Inspection() { }

    public static Inspection Create(
        Guid propertyId,
        string clientName,
        string clientEmail,
        string clientPhone,
        InspectionType type,
        DateTime preferredDateUtc,
        string preferredTimeSlot,
        int numberOfVisitors = 1,
        string? specialRequest = null,
        Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientPhone);

        return new Inspection
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            ClientName = clientName.Trim(),
            ClientEmail = clientEmail.Trim().ToLowerInvariant(),
            ClientPhone = clientPhone.Trim(),
            InspectionType = type,
            Status = InspectionStatus.Requested,
            PreferredDateUtc = preferredDateUtc,
            PreferredTimeSlot = preferredTimeSlot,
            NumberOfVisitors = numberOfVisitors,
            SpecialRequest = specialRequest
        };
    }

    public void AssignAgent(Guid agentId, string? meetingUrl = null)
    {
        AssignedAgentId = agentId;
        MeetingUrl = meetingUrl;
        Status = InspectionStatus.Confirmed;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted(string? notes = null)
    {
        Status = InspectionStatus.Completed;
        AgentNotes = notes;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = InspectionStatus.Cancelled;
        AgentNotes = reason;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

public sealed class Lead : AuditableEntity<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public Guid? InterestedPropertyId { get; private set; }
    public LeadSource Source { get; private set; } = LeadSource.Website;
    public LeadStatus Status { get; private set; } = LeadStatus.New;
    public string? BudgetRange { get; private set; }
    public string? Message { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public string? Notes { get; private set; }

    public Property? InterestedProperty { get; private set; }
    public User? AssignedAgent { get; private set; }

    private Lead() { }

    public static Lead Create(
        string fullName,
        string email,
        string phoneNumber,
        LeadSource source,
        Guid? interestedPropertyId = null,
        string? budgetRange = null,
        string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        return new Lead
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber.Trim(),
            Source = source,
            Status = LeadStatus.New,
            InterestedPropertyId = interestedPropertyId,
            BudgetRange = budgetRange,
            Message = message
        };
    }

    public void AssignAgent(Guid agentId)
    {
        AssignedAgentId = agentId;
        if (Status == LeadStatus.New) Status = LeadStatus.Contacted;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(LeadStatus newStatus, string? notes = null)
    {
        Status = newStatus;
        if (notes != null) Notes = notes;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

public sealed class AuditLog : BaseEntity<long>
{
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ChangesJson { get; private set; }
    public DateTime TimestampUtc { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        string entityName,
        string entityId,
        Guid? userId = null,
        string? userEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? changesJson = null)
    {
        return new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangesJson = changesJson,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
