using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZenithAssets.Application.Common.Interfaces;
using ZenithAssets.Application.Common.Models;
using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Application.Features.Properties;

public record PropertySummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string PropertyType,
    string Status,
    string DevelopmentStatus,
    string VerificationStatus,
    decimal PriceNaira,
    string FormattedPriceNaira,
    decimal? PriceUsd,
    string State,
    string City,
    string Area,
    int Bedrooms,
    int Bathrooms,
    int ParkingSpaces,
    decimal? LandSizeSqm,
    decimal? BuildingSizeSqm,
    bool IsFeatured,
    bool IsHotDeal,
    string? CoverImageUrl);

public record PropertyDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string PropertyType,
    string Status,
    string DevelopmentStatus,
    string VerificationStatus,
    decimal PriceNaira,
    string FormattedPriceNaira,
    decimal? PriceUsd,
    decimal? InitialDepositPercentage,
    int? MaxInstallmentMonths,
    string State,
    string City,
    string Area,
    string? Landmark,
    double? Latitude,
    double? Longitude,
    int Bedrooms,
    int Bathrooms,
    int ParkingSpaces,
    decimal? LandSizeSqm,
    decimal? BuildingSizeSqm,
    string? VirtualTour360Url,
    string? VideoTourUrl,
    List<string> Features,
    List<string> NearbyLandmarks,
    List<PropertyMediaDto> Media,
    List<PropertyDocumentMetadataDto> AvailableDocuments);

public record PropertyMediaDto(Guid Id, string MediaUrl, string MediaType, string? Caption, bool IsCover);
public record PropertyDocumentMetadataDto(Guid Id, string Title, string DocumentType, bool IsVerifiedByLegalTeam, int Version);

public record SearchPropertiesQuery(
    string? SearchTerm = null,
    string? State = null,
    string? City = null,
    string? Area = null,
    PropertyType? PropertyType = null,
    PropertyStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinBedrooms = null,
    bool? IsFeatured = null,
    int PageNumber = 1,
    int PageSize = 12) : IRequest<Result<PaginatedList<PropertySummaryDto>>>;

public class SearchPropertiesQueryHandler : IRequestHandler<SearchPropertiesQuery, Result<PaginatedList<PropertySummaryDto>>>
{
    private readonly IApplicationDbContext _context;

    public SearchPropertiesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<PropertySummaryDto>>> Handle(SearchPropertiesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Properties
            .AsNoTracking()
            .Include(p => p.Media)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) ||
                                     p.Description.ToLower().Contains(term) ||
                                     p.Address.Area.ToLower().Contains(term) ||
                                     p.Address.City.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            var state = request.State.Trim().ToLower();
            query = query.Where(p => p.Address.State.ToLower() == state);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(p => p.Address.City.ToLower() == city);
        }

        if (!string.IsNullOrWhiteSpace(request.Area))
        {
            var area = request.Area.Trim().ToLower();
            query = query.Where(p => p.Address.Area.ToLower().Contains(area));
        }

        if (request.PropertyType.HasValue)
        {
            query = query.Where(p => p.PropertyType == request.PropertyType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }
        else
        {
            // By default show available and reserved
            query = query.Where(p => p.Status == PropertyStatus.Available || p.Status == PropertyStatus.ReservedPending || p.Status == PropertyStatus.ReservedConfirmed);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.PriceNaira.Amount >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.PriceNaira.Amount <= request.MaxPrice.Value);
        }

        if (request.MinBedrooms.HasValue)
        {
            query = query.Where(p => p.Bedrooms >= request.MinBedrooms.Value);
        }

        if (request.IsFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.IsFeatured.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertySummaryDto(
                p.Id,
                p.Title,
                p.Slug,
                p.PropertyType.ToString(),
                p.Status.ToString(),
                p.DevelopmentStatus.ToString(),
                p.VerificationStatus.ToString(),
                p.PriceNaira.Amount,
                p.PriceNaira.FormatNaira(),
                p.PriceUsd != null ? p.PriceUsd.Amount : null,
                p.Address.State,
                p.Address.City,
                p.Address.Area,
                p.Bedrooms,
                p.Bathrooms,
                p.ParkingSpaces,
                p.LandSizeSqm,
                p.BuildingSizeSqm,
                p.IsFeatured,
                p.IsHotDeal,
                p.Media.Where(m => m.IsCover).Select(m => m.MediaUrl).FirstOrDefault() ?? p.Media.Select(m => m.MediaUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<PropertySummaryDto>(items, totalCount, pageNumber, pageSize);
        return Result<PaginatedList<PropertySummaryDto>>.Success(paginated);
    }
}

public record GetPropertyBySlugQuery(string Slug) : IRequest<Result<PropertyDetailDto>>;

public class GetPropertyBySlugQueryHandler : IRequestHandler<GetPropertyBySlugQuery, Result<PropertyDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPropertyBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PropertyDetailDto>> Handle(GetPropertyBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var property = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Media)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted, cancellationToken);

        if (property == null)
        {
            return Result<PropertyDetailDto>.Failure("Property not found.");
        }

        var mediaDtos = property.Media
            .OrderBy(m => m.SortOrder)
            .Select(m => new PropertyMediaDto(m.Id, m.MediaUrl, m.MediaType, m.Caption, m.IsCover))
            .ToList();

        var docDtos = property.Documents
            .Select(d => new PropertyDocumentMetadataDto(d.Id, d.Title, d.DocumentType.ToString(), d.IsVerifiedByLegalTeam, d.VersionNumber))
            .ToList();

        var dto = new PropertyDetailDto(
            property.Id,
            property.Title,
            property.Slug,
            property.Description,
            property.PropertyType.ToString(),
            property.Status.ToString(),
            property.DevelopmentStatus.ToString(),
            property.VerificationStatus.ToString(),
            property.PriceNaira.Amount,
            property.PriceNaira.FormatNaira(),
            property.PriceUsd?.Amount,
            property.InitialDepositPercentage,
            property.MaxInstallmentMonths,
            property.Address.State,
            property.Address.City,
            property.Address.Area,
            property.Address.Landmark,
            property.Coordinates.IsPubliclyExposed ? property.Coordinates.Latitude : null,
            property.Coordinates.IsPubliclyExposed ? property.Coordinates.Longitude : null,
            property.Bedrooms,
            property.Bathrooms,
            property.ParkingSpaces,
            property.LandSizeSqm,
            property.BuildingSizeSqm,
            property.VirtualTour360Url,
            property.VideoTourUrl,
            property.Features,
            property.NearbyLandmarks,
            mediaDtos,
            docDtos);

        return Result<PropertyDetailDto>.Success(dto);
    }
}

public record CreatePropertyCommand(
    string Title,
    string Slug,
    string Description,
    PropertyType PropertyType,
    DevelopmentStatus DevelopmentStatus,
    decimal PriceNaira,
    string State,
    string City,
    string Area,
    string? Landmark,
    double Latitude,
    double Longitude,
    bool ExposeCoordinatesPublicly = false,
    int Bedrooms = 0,
    int Bathrooms = 0,
    int ParkingSpaces = 0,
    decimal? LandSizeSqm = null,
    decimal? BuildingSizeSqm = null,
    decimal? PriceUsd = null,
    decimal InitialDepositPercentage = 30,
    int MaxInstallmentMonths = 12,
    string? VirtualTour360Url = null,
    string? VideoTourUrl = null) : IRequest<Result<Guid>>;

public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreatePropertyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await _context.Properties.AnyAsync(p => p.Slug == slug, cancellationToken);
        if (slugExists)
        {
            return Result<Guid>.Failure("A property with this URL slug already exists. Please provide a unique slug.");
        }

        var price = Money.Naira(request.PriceNaira);
        Money? usdPrice = request.PriceUsd.HasValue ? Money.USD(request.PriceUsd.Value) : null;
        var address = new Address(request.State, request.City, request.Area, request.Landmark);
        var coords = new GeoCoordinate(request.Latitude, request.Longitude, request.ExposeCoordinatesPublicly);

        var property = Property.Create(
            request.Title,
            slug,
            request.Description,
            request.PropertyType,
            request.DevelopmentStatus,
            price,
            address,
            coords,
            request.Bedrooms,
            request.Bathrooms,
            request.ParkingSpaces,
            request.LandSizeSqm,
            request.BuildingSizeSqm,
            usdPrice,
            request.InitialDepositPercentage,
            request.MaxInstallmentMonths);

        property.SetMediaUrls(request.VirtualTour360Url, request.VideoTourUrl);

        _context.Properties.Add(property);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(property.Id);
    }
}
