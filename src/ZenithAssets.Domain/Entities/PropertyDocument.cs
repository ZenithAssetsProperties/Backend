using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Domain.Entities;

public sealed class PropertyDocument : AuditableEntity<Guid>
{
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public string StorageKey { get; private set; } = string.Empty; // S3 object key
    public long FileSizeBytes { get; private set; }
    public string FileExtension { get; private set; } = ".pdf";
    public bool IsVerifiedByLegalTeam { get; private set; }
    public int VersionNumber { get; private set; } = 1;

    public Property Property { get; private set; } = null!;
    public ICollection<DocumentAccessRequest> AccessRequests { get; private set; } = [];

    private PropertyDocument() { }

    public static PropertyDocument Create(
        Guid propertyId,
        string title,
        DocumentType documentType,
        string storageKey,
        long fileSizeBytes,
        string fileExtension = ".pdf")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        return new PropertyDocument
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Title = title.Trim(),
            DocumentType = documentType,
            StorageKey = storageKey,
            FileSizeBytes = fileSizeBytes,
            FileExtension = fileExtension.ToLowerInvariant(),
            IsVerifiedByLegalTeam = false,
            VersionNumber = 1
        };
    }

    public void VerifyByLegal(string reviewerId)
    {
        IsVerifiedByLegalTeam = true;
        LastModifiedBy = reviewerId;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void IncrementVersion(string newStorageKey, long newSize)
    {
        StorageKey = newStorageKey;
        FileSizeBytes = newSize;
        VersionNumber++;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

public sealed class DocumentAccessRequest : AuditableEntity<Guid>
{
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public DocumentAccessStatus Status { get; private set; } = DocumentAccessStatus.PendingReview;
    public string? RequestReason { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public PropertyDocument Document { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private DocumentAccessRequest() { }

    public static DocumentAccessRequest Create(
        Guid documentId,
        Guid userId,
        string? requestReason = null)
    {
        return new DocumentAccessRequest
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            Status = DocumentAccessStatus.PendingReview,
            RequestReason = requestReason
        };
    }

    public void Approve(Guid adminId, int validHours = 72)
    {
        Status = DocumentAccessStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow.AddHours(validHours);
        LastModifiedBy = adminId.ToString();
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string reason)
    {
        Status = DocumentAccessStatus.Rejected;
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = DateTime.UtcNow;
        RejectionReason = reason;
        LastModifiedBy = adminId.ToString();
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public bool IsActive()
    {
        return Status == DocumentAccessStatus.Approved && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > DateTime.UtcNow);
    }
}

