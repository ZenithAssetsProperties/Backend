using ZenithAssets.Domain.Common;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Domain.Entities;

public sealed class Property : AuditableEntity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PropertyType PropertyType { get; private set; }
    public PropertyStatus Status { get; private set; }
    public DevelopmentStatus DevelopmentStatus { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public Money PriceNaira { get; private set; } = Money.Zero();
    public Money? PriceUsd { get; private set; }
    public decimal? InitialDepositPercentage { get; private set; }
    public int? MaxInstallmentMonths { get; private set; }

    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public int ParkingSpaces { get; private set; }
    public decimal? LandSizeSqm { get; private set; }
    public decimal? BuildingSizeSqm { get; private set; }

    public Address Address { get; private set; } = null!;
    public GeoCoordinate Coordinates { get; private set; } = null!;

    public bool IsFeatured { get; private set; }
    public bool IsHotDeal { get; private set; }
    public string? VirtualTour360Url { get; private set; }
    public string? VideoTourUrl { get; private set; }
    public List<string> Features { get; private set; } = [];
    public List<string> NearbyLandmarks { get; private set; } = [];

    // Navigation collections
    public ICollection<PropertyMedia> Media { get; private set; } = [];
    public ICollection<PropertyDocument> Documents { get; private set; } = [];
    public ICollection<Reservation> Reservations { get; private set; } = [];
    public ICollection<Inspection> Inspections { get; private set; } = [];

    private Property() { }

    public static Property Create(
        string title,
        string slug,
        string description,
        PropertyType propertyType,
        DevelopmentStatus developmentStatus,
        Money priceNaira,
        Address address,
        GeoCoordinate coordinates,
        int bedrooms = 0,
        int bathrooms = 0,
        int parkingSpaces = 0,
        decimal? landSizeSqm = null,
        decimal? buildingSizeSqm = null,
        Money? priceUsd = null,
        decimal? initialDepositPercentage = 30,
        int? maxInstallmentMonths = 12)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(coordinates);

        return new Property
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            PropertyType = propertyType,
            Status = PropertyStatus.Available,
            DevelopmentStatus = developmentStatus,
            VerificationStatus = VerificationStatus.Pending,
            PriceNaira = priceNaira,
            PriceUsd = priceUsd,
            Address = address,
            Coordinates = coordinates,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            ParkingSpaces = parkingSpaces,
            LandSizeSqm = landSizeSqm,
            BuildingSizeSqm = buildingSizeSqm,
            InitialDepositPercentage = initialDepositPercentage,
            MaxInstallmentMonths = maxInstallmentMonths
        };
    }

    public void MarkVerified(string verifiedBy)
    {
        VerificationStatus = VerificationStatus.ZenithVerified;
        LastModifiedBy = verifiedBy;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(PropertyStatus newStatus)
    {
        Status = newStatus;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void LockForReservation()
    {
        if (Status != PropertyStatus.Available)
            throw new InvalidOperationException($"Cannot reserve property with status '{Status}'. Only 'Available' properties can be reserved.");

        Status = PropertyStatus.ReservedPending;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void ConfirmReservation()
    {
        Status = PropertyStatus.ReservedConfirmed;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UnlockToAvailable()
    {
        Status = PropertyStatus.Available;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkSold()
    {
        Status = PropertyStatus.Sold;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void SetMediaUrls(string? virtualTourUrl, string? videoTourUrl)
    {
        VirtualTour360Url = virtualTourUrl;
        VideoTourUrl = videoTourUrl;
    }

    public void SetFlags(bool isFeatured, bool isHotDeal)
    {
        IsFeatured = isFeatured;
        IsHotDeal = isHotDeal;
    }
}

public sealed class PropertyMedia : AuditableEntity<Guid>
{
    public Guid PropertyId { get; private set; }
    public string MediaUrl { get; private set; } = string.Empty;
    public string MediaType { get; private set; } = "Photo"; // Photo, Video4k, DroneFootage, FloorPlan
    public string? Caption { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }

    public Property Property { get; private set; } = null!;

    private PropertyMedia() { }

    public static PropertyMedia Create(
        Guid propertyId,
        string mediaUrl,
        string mediaType = "Photo",
        string? caption = null,
        int sortOrder = 0,
        bool isCover = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaUrl);

        return new PropertyMedia
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            MediaUrl = mediaUrl.Trim(),
            MediaType = mediaType,
            Caption = caption,
            SortOrder = sortOrder,
            IsCover = isCover
        };
    }
}

