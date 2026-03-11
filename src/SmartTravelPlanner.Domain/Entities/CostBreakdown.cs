namespace SmartTravelPlanner.Domain.Entities;

public class CostBreakdown
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public decimal TotalActivitiesCost { get; set; }
    public decimal TotalDiningCost { get; set; }
    public decimal TotalTransportCost { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal RemainingBudget { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Navigation
    public Itinerary Itinerary { get; set; } = null!;

    public bool IsValid()
    {
        var expectedGrandTotal = TotalActivitiesCost + TotalDiningCost + TotalTransportCost;
        return GrandTotal == expectedGrandTotal && GrandTotal >= 0 && RemainingBudget >= 0;
    }
}
