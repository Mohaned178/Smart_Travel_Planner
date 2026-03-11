using FluentValidation;
using SmartTravelPlanner.Application.DTOs;

namespace SmartTravelPlanner.Application.Validators;

public class GenerateItineraryCommandValidator : AbstractValidator<GenerateItineraryCommand>
{
    private static readonly HashSet<string> ValidInterests = new(StringComparer.OrdinalIgnoreCase)
    {
        "museums", "parks", "food", "nightlife", "shopping",
        "history", "landmarks", "adventure", "beaches", "art", "nature"
    };

    public GenerateItineraryCommandValidator()
    {
        RuleFor(x => x.CityName)
            .NotEmpty().WithMessage("City name is required.")
            .MaximumLength(200).WithMessage("City name must not exceed 200 characters.");

        RuleFor(x => x.TotalBudget)
            .GreaterThan(0).WithMessage("Total budget must be greater than 0.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Matches("^[A-Z]{3}$").WithMessage("Currency code must be a valid 3-letter ISO 4217 code.");

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 14).WithMessage("Duration must be between 1 and 14 days.");

        RuleFor(x => x.TripStartDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Trip start date must be today or a future date.");

        RuleFor(x => x.Interests)
            .NotEmpty().WithMessage("At least one interest is required.")
            .Must(interests => interests.All(i => ValidInterests.Contains(i)))
            .WithMessage("All interests must be from the predefined catalog.");
    }
}
