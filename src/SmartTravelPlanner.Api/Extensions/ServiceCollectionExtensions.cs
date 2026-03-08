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
using SmartTravelPlanner.Infrastructure.ExternalApis.Frankfurter;
using SmartTravelPlanner.Infrastructure.ExternalApis.Nominatim;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenMeteo;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenRouteService;
using SmartTravelPlanner.Infrastructure.ExternalApis.OpenTripMap;
using SmartTravelPlanner.Infrastructure.Persistence;
using SmartTravelPlanner.Infrastructure.Persistence.Repositories;

namespace SmartTravelPlanner.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // Identity
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

        // JWT Authentication
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

        // Auth services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Application services
        services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
        services.AddScoped<ICostCalculationService, CostCalculationService>();
        services.AddScoped<IItineraryGenerationService, ItineraryGenerationService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<GenerateItineraryCommandValidator>();

        // Repositories
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlacesCacheRepository, PlacesCacheRepository>();

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<InMemoryCacheService>();

        // External API HttpClients with Polly resilience
        services.AddHttpClient<IGeocodingClient, NominatimGeocodingClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<ICurrencyClient, FrankfurterCurrencyClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<IPlacesClient, OpenTripMapPlacesClient>()
            .AddResiliencePolicies();
        services.AddHttpClient<IRoutingClient, OpenRouteServiceRoutingClient>()
            .AddResiliencePolicies();

        return services;
    }
}
