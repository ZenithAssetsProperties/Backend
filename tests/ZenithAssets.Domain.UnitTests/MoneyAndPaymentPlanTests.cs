using ZenithAssets.Domain.Entities;
using ZenithAssets.Domain.Enums;
using ZenithAssets.Domain.ValueObjects;

namespace ZenithAssets.Domain.UnitTests;

public class MoneyAndPaymentPlanTests
{
    [Fact]
    public void Money_ShouldAddAndSubtract_WhenSameCurrency()
    {
        var m1 = Money.Naira(50000000m); // 50m
        var m2 = Money.Naira(35000000m); // 35m

        var total = m1.Add(m2);
        var diff = m1.Subtract(m2);

        Assert.Equal(85000000m, total.Amount);
        Assert.Equal("NGN", total.Currency);
        Assert.Equal(15000000m, diff.Amount);
    }

    [Fact]
    public void Money_ShouldThrowException_WhenCurrenciesMismatch()
    {
        var naira = Money.Naira(100000);
        var usd = Money.USD(100);

        Assert.Throws<InvalidOperationException>(() => naira.Add(usd));
    }

    [Fact]
    public void PaymentPlan_ShouldComputeCorrectBalanceAndMonthlyInstallments()
    {
        var propertyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var totalPrice = Money.Naira(120000000m); // 120m
        var deposit = Money.Naira(36000000m);     // 30% = 36m
        var tenureMonths = 12;

        var plan = PaymentPlan.Create(propertyId, userId, totalPrice, deposit, tenureMonths);

        Assert.Equal(84000000m, plan.BalanceRemaining.Amount); // 120m - 36m = 84m
        Assert.Equal(7000000m, plan.MonthlyInstallmentAmount.Amount); // 84m / 12 = 7m
        Assert.False(plan.IsCompleted);

        // Record 1 month payment
        plan.RecordInstallmentPayment(Money.Naira(7000000m));
        Assert.Equal(77000000m, plan.BalanceRemaining.Amount);
    }

    [Fact]
    public void Property_ShouldOnlyAllowReservation_WhenAvailable()
    {
        var property = Property.Create(
            "Luxury 4-Bedroom Duplex",
            "luxury-4-bedroom-duplex-lekki",
            "Stunning contemporary duplex in Lekki Phase 1",
            PropertyType.ResidentialHouse,
            DevelopmentStatus.Completed,
            Money.Naira(185000000m),
            new Address("Lagos", "Lekki", "Lekki Phase 1"),
            new GeoCoordinate(6.4474, 3.4723, false),
            bedrooms: 4,
            bathrooms: 4
        );

        Assert.Equal(PropertyStatus.Available, property.Status);

        property.LockForReservation();
        Assert.Equal(PropertyStatus.ReservedPending, property.Status);

        // Attempting to lock again must throw
        Assert.Throws<InvalidOperationException>(() => property.LockForReservation());

        // Confirm reservation
        property.ConfirmReservation();
        Assert.Equal(PropertyStatus.ReservedConfirmed, property.Status);
    }
}
