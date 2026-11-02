using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Features.Leads;

public record CaptureLeadCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    LeadSource Source,
    Guid? InterestedPropertyId = null,
    string? BudgetRange = null,
    string? Message = null) : IRequest<Result<Guid>>;

public class CaptureLeadCommandHandler : IRequestHandler<CaptureLeadCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CaptureLeadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CaptureLeadCommand request, CancellationToken cancellationToken)
    {
        var lead = Lead.Create(
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.Source,
            request.InterestedPropertyId,
            request.BudgetRange,
            request.Message);

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(lead.Id);
    }
}
