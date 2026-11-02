using ZenithAssets.Domain.Common;

namespace ZenithAssets.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { Amount = 0; Currency = "NGN"; }

    public Money(decimal amount, string currency = "NGN")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified.", nameof(currency));

        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "NGN") => new(0, currency);
    public static Money Naira(decimal amount) => new(amount, "NGN");
    public static Money USD(decimal amount) => new(amount, "USD");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount)
            throw new InvalidOperationException("Cannot subtract a larger amount resulting in negative money.");
        return new Money(Amount - other.Amount, Currency);
    }

    public string FormatNaira()
    {
        if (Currency == "NGN")
            return $"₦{Amount:N0}";
        return $"{Currency} {Amount:N2}";
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on money with different currencies: {Currency} and {other.Currency}");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public sealed class Address : ValueObject
{
    public string State { get; }
    public string City { get; }
    public string Area { get; }
    public string? Landmark { get; }
    public string? FullAddress { get; }

    private Address() { State = ""; City = ""; Area = ""; }

    public Address(string state, string city, string area, string? landmark = null, string? fullAddress = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Area = area ?? throw new ArgumentNullException(nameof(area));
        Landmark = landmark;
        FullAddress = fullAddress;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return State.ToLowerInvariant();
        yield return City.ToLowerInvariant();
        yield return Area.ToLowerInvariant();
        yield return Landmark?.ToLowerInvariant();
    }
}

public sealed class GeoCoordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    public bool IsPubliclyExposed { get; }

    private GeoCoordinate() { }

    public GeoCoordinate(double latitude, double longitude, bool isPubliclyExposed = false)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
        IsPubliclyExposed = isPubliclyExposed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return IsPubliclyExposed;
    }
}

