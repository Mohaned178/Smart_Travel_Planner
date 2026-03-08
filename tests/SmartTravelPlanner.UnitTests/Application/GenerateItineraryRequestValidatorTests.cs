using FluentAssertions;
using SmartTravelPlanner.Application.DTOs;
using SmartTravelPlanner.Application.Validators;

namespace SmartTravelPlanner.UnitTests.Application;

public class GenerateItineraryRequestValidatorTests
{
    private readonly GenerateItineraryCommandValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = CreateValidRequest();
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CityName_Fails()
    {
        var request = CreateValidRequest() with { CityName = "" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CityName");
    }

    [Fact]
    public void Budget_Zero_Fails()
    {
        var request = CreateValidRequest() with { TotalBudget = 0 };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalBudget");
    }

    [Fact]
    public void Budget_Negative_Fails()
    {
        var request = CreateValidRequest() with { TotalBudget = -100 };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(-1)]
    public void Duration_OutOfRange_Fails(int days)
    {
        var request = CreateValidRequest() with { DurationDays = days };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationDays");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void Duration_InRange_Passes(int days)
    {
        var request = CreateValidRequest() with { DurationDays = days };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Interests_Fails()
    {
        var request = CreateValidRequest() with { Interests = [] };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Interests");
    }

    [Fact]
    public void Invalid_Interest_Fails()
    {
        var request = CreateValidRequest() with { Interests = ["invalid_interest"] };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_CurrencyCode_Fails()
    {
        var request = CreateValidRequest() with { CurrencyCode = "usd" };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PastDate_Fails()
    {
        var request = CreateValidRequest() with { TripStartDate = DateTime.Today.AddDays(-1) };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Today_Date_Passes()
    {
        var request = CreateValidRequest() with { TripStartDate = DateTime.Today };
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    private static GenerateItineraryCommand CreateValidRequest() => new()
    {
        CityName = "Paris",
        TotalBudget = 500,
        CurrencyCode = "USD",
        DurationDays = 3,
        TripStartDate = DateTime.Today.AddDays(7),
        Interests = ["museums", "parks", "food"]
    };
}
