using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Features.Documents;

public record RequestDocumentAccessCommand(
    Guid DocumentId,
    string? Reason = null) : IRequest<Result<Guid>>;

public class RequestDocumentAccessCommandHandler : IRequestHandler<RequestDocumentAccessCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RequestDocumentAccessCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(RequestDocumentAccessCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result<Guid>.Failure("User must be authenticated to request document access.");
        }

        var doc = await _context.PropertyDocuments.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);
        if (doc == null)
        {
            return Result<Guid>.Failure("Property document not found.");
        }

        // Check if an existing approved request exists
        var existing = await _context.DocumentAccessRequests
            .FirstOrDefaultAsync(r => r.DocumentId == request.DocumentId && r.UserId == _currentUser.UserId.Value, cancellationToken);

        if (existing != null && existing.IsActive())
        {
            return Result<Guid>.Success(existing.Id);
        }

        var accessRequest = DocumentAccessRequest.Create(request.DocumentId, _currentUser.UserId.Value, request.Reason);
        _context.DocumentAccessRequests.Add(accessRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(accessRequest.Id);
    }
}

public record ApproveDocumentAccessCommand(
    Guid RequestId,
    int ValidHours = 72) : IRequest<Result>;

public class ApproveDocumentAccessCommandHandler : IRequestHandler<ApproveDocumentAccessCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveDocumentAccessCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ApproveDocumentAccessCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin)
        {
            return Result.Failure("Only Zenith Administrators can approve document access.");
        }

        var accessRequest = await _context.DocumentAccessRequests
            .Include(r => r.Document)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (accessRequest == null)
        {
            return Result.Failure("Document access request not found.");
        }

        accessRequest.Approve(_currentUser.UserId!.Value, request.ValidHours);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public record GetSecureDocumentUrlQuery(Guid DocumentId) : IRequest<Result<string>>;

public class GetSecureDocumentUrlQueryHandler : IRequestHandler<GetSecureDocumentUrlQuery, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;

    public GetSecureDocumentUrlQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStorageService storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<string>> Handle(GetSecureDocumentUrlQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result<string>.Failure("Unauthorized access.");
        }

        var doc = await _context.PropertyDocuments
            .Include(d => d.Property)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (doc == null)
        {
            return Result<string>.Failure("Document not found.");
        }

        // Admins have automatic access
        var isAdmin = _currentUser.Role is UserRole.Admin or UserRole.SuperAdmin;

        if (!isAdmin)
        {
            // Verify buyer has approved active access or owns reservation/purchase
            var hasAccess = await _context.DocumentAccessRequests
                .AnyAsync(r => r.DocumentId == request.DocumentId &&
                               r.UserId == _currentUser.UserId.Value &&
                               r.Status == DocumentAccessStatus.Approved &&
                               (!r.ExpiresAtUtc.HasValue || r.ExpiresAtUtc.Value > DateTime.UtcNow), cancellationToken);

            if (!hasAccess)
            {
                return Result<string>.Failure("Access to this confidential title document has not been approved or has expired. Please request access.");
            }
        }

        // Generate 5-minute Pre-Signed URL with download filename
        var safeFileName = $"{doc.Property.Slug}-{doc.DocumentType}{doc.FileExtension}";
        var preSignedUrl = await _storage.GeneratePreSignedDownloadUrlAsync(doc.StorageKey, TimeSpan.FromMinutes(5), safeFileName);

        return Result<string>.Success(preSignedUrl);
    }
}

