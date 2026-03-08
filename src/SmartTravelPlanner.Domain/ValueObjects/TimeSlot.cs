namespace SmartTravelPlanner.Domain.ValueObjects;

/// <summary>
/// Represents a time window within a single day.
/// </summary>
public sealed record TimeSlot
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeSlot(TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            throw new ArgumentException($"TimeSlot Start ({start}) must be before End ({end}).");

        Start = start;
        End = end;
    }

    public int DurationMinutes => (int)(End - Start).TotalMinutes;
}
