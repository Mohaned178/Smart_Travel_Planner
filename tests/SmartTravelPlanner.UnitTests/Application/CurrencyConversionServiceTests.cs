using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Domain.Interfaces;

namespace SmartTravelPlanner.UnitTests.Application;

public class CurrencyConversionServiceTests
{
    private readonly Mock<ICurrencyClient> _currencyClientMock;
    private readonly CurrencyConversionService _service;

    public CurrencyConversionServiceTests()
    {
        _currencyClientMock = new Mock<ICurrencyClient>();
        _service = new CurrencyConversionService(_currencyClientMock.Object, new NullLogger<CurrencyConversionService>());
    }

    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsOriginalAmount()
    {
        var (amount, fallback) = await _service.ConvertAsync(100m, "USD", "USD");
        
        amount.Should().Be(100m);
        fallback.Should().BeFalse();
        _currencyClientMock.Verify(x => x.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ConvertAsync_DifferentCurrency_UsesRate()
    {
        _currencyClientMock.Setup(x => x.GetExchangeRateAsync("EUR", "USD", default))
            .ReturnsAsync(1.10m);

        var (amount, fallback) = await _service.ConvertAsync(100m, "EUR", "USD");

        amount.Should().Be(110.00m);
        fallback.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertAsync_ApiUnavailable_UsesFallback()
    {
        _currencyClientMock.Setup(x => x.GetExchangeRateAsync("EUR", "JPY", default))
            .ReturnsAsync((decimal?)null);

        var (amount, fallback) = await _service.ConvertAsync(100m, "EUR", "JPY");

        amount.Should().Be(100m);
        fallback.Should().BeTrue();
    }
}
