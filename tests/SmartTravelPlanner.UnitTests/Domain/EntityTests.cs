using FluentAssertions;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.ValueObjects;

namespace SmartTravelPlanner.UnitTests.Domain;

public class EntityTests
{
    [Fact]
    public void Coordinates_DistanceToKm_CalculatesCorrectly()
    {
        var paris = new Coordinates(48.8566m, 2.3522m);
        var london = new Coordinates(51.5074m, -0.1278m);

        var distance = paris.DistanceToKm(london);
        distance.Should().BeInRange(340m, 350m);
    }

    [Fact]
    public void Coordinates_SamePoint_ZeroDistance()
    {
        var point = new Coordinates(48.8566m, 2.3522m);
        var distance = point.DistanceToKm(point);
        distance.Should().Be(0);
    }

    [Fact]
    public void Money_Add_SameCurrency_Works()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "USD");
        var result = a.Add(b);
        result.Amount.Should().Be(30m);
        result.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Money_Add_DifferentCurrency_Throws()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "EUR");
        var act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Money_Subtract_Works()
    {
        var a = new Money(30m, "USD");
        var b = new Money(10m, "USD");
        var result = a.Subtract(b);
        result.Amount.Should().Be(20m);
    }

    [Fact]
    public void TimeSlot_Valid_CreatesSuccessfully()
    {
        var slot = new TimeSlot(new TimeOnly(9, 0), new TimeOnly(11, 0));
        slot.Start.Should().Be(new TimeOnly(9, 0));
        slot.End.Should().Be(new TimeOnly(11, 0));
    }

    [Fact]
    public void TimeSlot_EndBeforeStart_Throws()
    {
        var act = () => new TimeSlot(new TimeOnly(11, 0), new TimeOnly(9, 0));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CostBreakdown_ValidInvariants_ReturnsTrue()
    {
        var breakdown = new CostBreakdown
        {
            TotalActivitiesCost = 100, TotalDiningCost = 50, TotalTransportCost = 30,
            GrandTotal = 180, RemainingBudget = 320, CurrencyCode = "USD"
        };
        breakdown.IsValid().Should().BeTrue();
    }

    [Fact]
    public void CostBreakdown_MismatchedGrandTotal_ReturnsFalse()
    {
        var breakdown = new CostBreakdown
        {
            TotalActivitiesCost = 100, TotalDiningCost = 50, TotalTransportCost = 30,
            GrandTotal = 999, RemainingBudget = 0, CurrencyCode = "USD"
        };
        breakdown.IsValid().Should().BeFalse();
    }

    [Fact]
    public void CostBreakdown_NegativeRemaining_ReturnsFalse()
    {
        var breakdown = new CostBreakdown
        {
            TotalActivitiesCost = 100, TotalDiningCost = 50, TotalTransportCost = 30,
            GrandTotal = 180, RemainingBudget = -10, CurrencyCode = "USD"
        };
        breakdown.IsValid().Should().BeFalse();
    }
}
