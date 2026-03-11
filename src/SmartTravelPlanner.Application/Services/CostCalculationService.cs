using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Application.Services;

public interface ICostCalculationService
{
    CostBreakdown CalculateCostBreakdown(Itinerary itinerary, decimal transportRatePerKm);
    decimal EstimateTransportCost(decimal totalDistanceKm, decimal ratePerKm);
}

public class CostCalculationService : ICostCalculationService
{
    private const decimal MetroFlatFare = 2.50m;
    private const decimal TaxiBaseFare = 3.00m;
    private const decimal TaxiPerKmRate = 2.00m;

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
                {
                    dayTransportCost += EstimateTransportCostByMode(
                        activity.TravelDistanceFromPrevKm.Value,
                        activity.TransportMode,
                        transportRatePerKm);
                }
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

    private static decimal EstimateTransportCostByMode(decimal distanceKm, string? transportMode, decimal fallbackRatePerKm)
    {
        return transportMode switch
        {
            "Walking" => 0m,
            "Metro" => MetroFlatFare,
            "Taxi" => Math.Round(TaxiBaseFare + distanceKm * TaxiPerKmRate, 2),
            _ => Math.Round(distanceKm * fallbackRatePerKm, 2)
        };
    }

    public decimal EstimateTransportCost(decimal totalDistanceKm, decimal ratePerKm)
        => Math.Round(totalDistanceKm * ratePerKm, 2);
}
