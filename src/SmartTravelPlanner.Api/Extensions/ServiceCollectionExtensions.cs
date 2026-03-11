using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTravelPlanner.Application.Interfaces;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Application.Validators;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Infrastructure.Auth;
using SmartTravelPlanner.Infrastructure.Caching;
using SmartTravelPlanner.Infrastructure.Extensions;
using SmartTravelPlanner.Infrastructure.ExternalApis.GooglePlaces;
using SmartTravelPlanner.Infrastructure.ExternalApis.Frankfurter;
using SmartTravelPlanner.Infrastructure.ExternalApis.Nominatim;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenWeather;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenRouteService;
using SmartTravelPlanner.Infrastructure.Persistence;
using SmartTravelPlanner.Infrastructure.Persistence.Repositories;

namespace SmartTravelPlanner.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var jwtSecret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
        services.AddScoped<ICostCalculationService, CostCalculationService>();
        services.AddScoped<IItineraryGenerationService, ItineraryGenerationService>();

        services.AddValidatorsFromAssemblyContaining<GenerateItineraryCommandValidator>();

        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlacesCacheRepository, PlacesCacheRepository>();

        services.AddMemoryCache();
        services.AddSingleton<InMemoryCacheService>();

        services.AddHttpClient<IGeocodingClient, NominatimGeocodingClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<ICurrencyClient, FrankfurterCurrencyClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<IWeatherClient, OpenWeatherClient>(client =>
        {
            var baseUrl = config["ExternalApis:OpenWeather:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        })
            .AddResiliencePolicies();
        services.AddHttpClient<IPlacesClient, GooglePlacesClient>(client =>
        {
            var baseUrl = config["ExternalApis:GooglePlaces:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        })
        .AddResiliencePolicies();
        services.AddHttpClient<IRoutingClient, OpenRouteServiceRoutingClient>()
            .AddResiliencePolicies();

        return services;
    }
}
