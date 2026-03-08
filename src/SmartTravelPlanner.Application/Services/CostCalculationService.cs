using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Application.Services;

public interface ICostCalculationService
{
    CostBreakdown CalculateCostBreakdown(Itinerary itinerary, decimal transportRatePerKm);
    decimal EstimateTransportCost(decimal totalDistanceKm, decimal ratePerKm);
}

/// <summary>
/// Budget distribution and cost breakdown computation per FR-005, FR-011.
/// </summary>
public class CostCalculationService : ICostCalculationService
{
    private readonly ILogger<CostCalculationService> _logger;

    public CostCalculationService(ILogger<CostCalculationService> logger)
    {
        _logger = logger;
    }

    public CostBreakdown CalculateCostBreakdown(Itinerary itinerary, decimal transportRatePerKm)
    {
        decimal totalActivitiesCost = 0;
        decimal totalDiningCost = 0;
        decimal totalTransportCost = 0;

        foreach (var dayPlan in itinerary.DayPlans)
        {
            decimal dayActivityCost = 0;
            decimal dayDiningCost = 0;
            decimal dayTransportCost = 0;

            foreach (var activity in dayPlan.Activities)
            {
                dayActivityCost += activity.EstimatedCostUser;

                if (activity.TravelDistanceFromPrevKm.HasValue)
                    dayTransportCost += EstimateTransportCost(activity.TravelDistanceFromPrevKm.Value, transportRatePerKm);
            }

            foreach (var restaurant in dayPlan.Restaurants)
                dayDiningCost += restaurant.EstimatedMealCost;

            dayPlan.DailyCostTotal = dayActivityCost + dayDiningCost + dayTransportCost;
            totalActivitiesCost += dayActivityCost;
            totalDiningCost += dayDiningCost;
            totalTransportCost += dayTransportCost;
        }

        var grandTotal = totalActivitiesCost + totalDiningCost + totalTransportCost;
        var remaining = itinerary.TotalBudget - grandTotal;

        _logger.LogInformation(
            "Cost breakdown: activities={Activities}, dining={Dining}, transport={Transport}, total={Total}, remaining={Remaining}",
            totalActivitiesCost, totalDiningCost, totalTransportCost, grandTotal, remaining);

        return new CostBreakdown
        {
            ItineraryId = itinerary.Id,
            TotalActivitiesCost = totalActivitiesCost,
            TotalDiningCost = totalDiningCost,
            TotalTransportCost = totalTransportCost,
            GrandTotal = grandTotal,
            RemainingBudget = Math.Max(0, remaining),
            CurrencyCode = itinerary.CurrencyCode
        };
    }

    public decimal EstimateTransportCost(decimal totalDistanceKm, decimal ratePerKm)
        => Math.Round(totalDistanceKm * ratePerKm, 2);
}
