using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.UnitTests.Application;

public class CostCalculationServiceTests
{
    private readonly CostCalculationService _sut;

    public CostCalculationServiceTests()
    {
        _sut = new CostCalculationService(Mock.Of<ILogger<CostCalculationService>>());
    }

    [Fact]
    public void CalculateCostBreakdown_DistributesBudget_SumsCorrectly()
    {
        
        var itinerary = CreateItinerary(budget: 500, days: 3);

        
        var result = _sut.CalculateCostBreakdown(itinerary, 0.50m);

        
        result.GrandTotal.Should().Be(result.TotalActivitiesCost + result.TotalDiningCost + result.TotalTransportCost);
        result.RemainingBudget.Should().Be(Math.Max(0, 500 - result.GrandTotal));
    }

    [Fact]
    public void CalculateCostBreakdown_GrandTotalWithinBudget()
    {
        
        var itinerary = CreateItinerary(budget: 500, days: 2);

        
        var result = _sut.CalculateCostBreakdown(itinerary, 0.50m);

        
        result.GrandTotal.Should().BeLessThanOrEqualTo(500);
        result.RemainingBudget.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CalculateCostBreakdown_PerDayCosts_SumToCategories()
    {
        
        var itinerary = CreateItinerary(budget: 1000, days: 3, activitiesPerDay: 4);

        
        var result = _sut.CalculateCostBreakdown(itinerary, 0.50m);

        
        var dailyTotalsSum = itinerary.DayPlans.Sum(dp => dp.DailyCostTotal);
        dailyTotalsSum.Should().Be(result.GrandTotal);
    }

    [Fact]
    public void CalculateCostBreakdown_RemainingBudget_NeverNegative()
    {
        var itinerary = CreateItinerary(budget: 10, days: 3, activityCost: 100);

        
        var result = _sut.CalculateCostBreakdown(itinerary, 0.50m);

        
        result.RemainingBudget.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void EstimateTransportCost_CalculatesCorrectly()
    {
        var cost = _sut.EstimateTransportCost(10m, 0.50m);
        cost.Should().Be(5.00m);
    }

    private static Itinerary CreateItinerary(decimal budget = 500, int days = 3,
        int activitiesPerDay = 3, decimal activityCost = 15, decimal mealCost = 12)
    {
        var itinerary = new Itinerary
        {
            TotalBudget = budget,
            CurrencyCode = "USD",
            DurationDays = days,
            DayPlans = new List<DayPlan>()
        };

        for (int d = 0; d < days; d++)
        {
            var dayPlan = new DayPlan
            {
                DayNumber = d + 1,
                Activities = new List<ActivitySlot>(),
                Restaurants = new List<RestaurantSuggestion>()
            };

            for (int a = 0; a < activitiesPerDay; a++)
            {
                dayPlan.Activities.Add(new ActivitySlot
                {
                    EstimatedCostUser = activityCost,
                    TravelDistanceFromPrevKm = a > 0 ? 2m : null
                });
            }

            dayPlan.Restaurants.Add(new RestaurantSuggestion { EstimatedMealCost = mealCost });

            itinerary.DayPlans.Add(dayPlan);
        }

        return itinerary;
    }
}
