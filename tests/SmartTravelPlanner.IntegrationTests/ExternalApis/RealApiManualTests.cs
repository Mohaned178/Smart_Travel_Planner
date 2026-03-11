using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTravelPlanner.Domain.ValueObjects;
using SmartTravelPlanner.Infrastructure.ExternalApis.GooglePlaces;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenWeather;

namespace SmartTravelPlanner.IntegrationTests.ExternalApis;

public class RealApiManualTests
{
    private readonly ITestOutputHelper _output;

    public RealApiManualTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestRealApis()
    {
        var googleKey = "dummy_google_key";
        var openWeatherKey = "dummy_openweather_key";

        var configGoogle = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("ExternalApis:GooglePlaces:ApiKey", googleKey) }).Build();
        var configOpenWeather = new ConfigurationBuilder().AddInMemoryCollection(new[] { new KeyValuePair<string, string>("ExternalApis:OpenWeather:ApiKey", openWeatherKey) }).Build();
        
        var httpClientGoogle = new HttpClient() { BaseAddress = new Uri("https://maps.googleapis.com/maps/api/place/") };
        var googleClient = new GooglePlacesClient(httpClientGoogle, configGoogle, new NullLogger<GooglePlacesClient>());

        var httpClientOpenWeather = new HttpClient() { BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/") };
        var openWeatherClient = new OpenWeatherClient(httpClientOpenWeather, configOpenWeather, new NullLogger<OpenWeatherClient>());


        _output.WriteLine("--- Testing Madrid, Spain ---");
        var madridCoords = new Coordinates(40.4168m, -3.7038m);
        await TestLocation(madridCoords, googleClient, openWeatherClient);

        _output.WriteLine("\n--- Testing Cairo, Egypt ---");
        var cairoCoords = new Coordinates(30.0444m, 31.2357m);
        await TestLocation(cairoCoords, googleClient, openWeatherClient);
        
        _output.WriteLine("\n--- Testing Paris, France ---");
        var parisCoords = new Coordinates(48.8566m, 2.3522m);
        await TestLocation(parisCoords, googleClient, openWeatherClient);
    }

    private async Task TestLocation(Coordinates coords, GooglePlacesClient placesClient, OpenWeatherClient owClient)
    {
        _output.WriteLine("Fetching places from Google Places...");
        var keyword = "tourist_attraction";
        var radius = 5000;
        var url = $"nearbysearch/json?location={Math.Round(coords.Latitude, 6)},{Math.Round(coords.Longitude, 6)}&radius={radius}&keyword={Uri.EscapeDataString(keyword)}&key=dummy_google_key";
        
        var request = new HttpRequestMessage(HttpMethod.Get, "https://maps.googleapis.com/maps/api/place/" + url);
        request.Headers.Add("Accept", "application/json");

        try 
        {
            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);
            _output.WriteLine($"Google status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Google content starts with: {(content.Length > 100 ? content.Substring(0, 100) : content)}...");

            var places = await placesClient.SearchPlacesAsync(coords, 5000, new[] { "tourist_attraction" });
            _output.WriteLine($"Parsed {places.Count} places.");
            foreach (var place in places.Take(3))
            {
                _output.WriteLine($"- {place.Name} ({place.Category}) @ {place.Address} [Rating: {place.Rating}, Cost: {place.EstimatedCost}]");
            }
        }
        catch(Exception e)
        {
             _output.WriteLine($"Google errored: {e.Message}");
        }

        _output.WriteLine("\nFetching weather from OpenWeather...");
        var forecasts = await owClient.GetForecastAsync(coords, 3);
        
        _output.WriteLine($"Got {forecasts.Count} days of forecast.");
        var day = 1;
        foreach (var forecast in forecasts)
        {
            _output.WriteLine($"- Day {day++}: {forecast.MinTemp}°C to {forecast.MaxTemp}°C, Precipitation: {forecast.Precipitation}mm, Code: {forecast.Code}");
        }
    }
}
