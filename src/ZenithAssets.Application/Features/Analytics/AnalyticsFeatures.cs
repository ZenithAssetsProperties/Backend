using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Enums;

namespace ZenithAssets.Application.Features.Analytics;

public record ExecutiveAnalyticsDto(
    int TotalPropertiesCount,
    int AvailablePropertiesCount,
    int ReservedPropertiesCount,
    int SoldPropertiesCount,
    decimal TotalPortfolioValueNaira,
    string FormattedTotalPortfolioValue,
    decimal TotalRevenueCollectedNaira,
    string FormattedTotalRevenue,
    int TotalRegisteredBuyers,
    int TotalInspectionsBooked,
    int ActiveLeadsCount);

public record GetExecutiveAnalyticsQuery : IRequest<Result<ExecutiveAnalyticsDto>>;

public class GetExecutiveAnalyticsQueryHandler : IRequestHandler<GetExecutiveAnalyticsQuery, Result<ExecutiveAnalyticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetExecutiveAnalyticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ExecutiveAnalyticsDto>> Handle(GetExecutiveAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.SuperAdmin)
        {
            return Result<ExecutiveAnalyticsDto>.Failure("Access restricted to Zenith Administrators.");
        }

        var properties = await _context.Properties.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync(cancellationToken);
        var totalProps = properties.Count;
        var available = properties.Count(p => p.Status == PropertyStatus.Available);
        var reserved = properties.Count(p => p.Status is PropertyStatus.ReservedPending or PropertyStatus.ReservedConfirmed);
        var sold = properties.Count(p => p.Status == PropertyStatus.Sold);
        var portfolioVal = properties.Sum(p => p.PriceNaira.Amount);

        var totalRevenue = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Status == PaymentStatus.Success)
            .SumAsync(t => t.Amount.Amount, cancellationToken);

        var buyersCount = await _context.Users.AsNoTracking().CountAsync(u => u.Role == UserRole.Buyer || u.Role == UserRole.DiasporaBuyer, cancellationToken);
        var inspectionsCount = await _context.Inspections.AsNoTracking().CountAsync(cancellationToken);
        var activeLeads = await _context.Leads.AsNoTracking().CountAsync(l => l.Status != LeadStatus.ClosedLost && l.Status != LeadStatus.ClosedWon, cancellationToken);

        var dto = new ExecutiveAnalyticsDto(
            totalProps,
            available,
            reserved,
            sold,
            portfolioVal,
            $"₦{portfolioVal:N0}",
            totalRevenue,
            $"₦{totalRevenue:N0}",
            buyersCount,
            inspectionsCount,
            activeLeads);

        return Result<ExecutiveAnalyticsDto>.Success(dto);
    }
}

