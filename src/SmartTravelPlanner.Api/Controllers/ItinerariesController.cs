using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTravelPlanner.Api.DTOs.Requests;
using SmartTravelPlanner.Api.DTOs.Responses;
using SmartTravelPlanner.Application.DTOs;
using SmartTravelPlanner.Application.Services;
using SmartTravelPlanner.Domain.Interfaces;
using SmartTravelPlanner.Infrastructure.Persistence;

namespace SmartTravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItinerariesController : ControllerBase
{
    private readonly IItineraryGenerationService _generationService;
    private readonly IItineraryRepository _itineraryRepo;
    private readonly IValidator<GenerateItineraryCommand> _validator;
    private readonly AppDbContext _dbContext;

    public ItinerariesController(
        IItineraryGenerationService generationService,
        IItineraryRepository itineraryRepo,
        IValidator<GenerateItineraryCommand> validator,
        AppDbContext dbContext)
    {
        _generationService = generationService;
        _itineraryRepo = itineraryRepo;
        _validator = validator;
        _dbContext = dbContext;
    }

    [HttpGet("interests")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInterests()
    {
        var interests = _dbContext.Interests.Select(i => new
        {
            i.Name,
            i.Category,
            i.DisplayName
        }).ToList();

        return Ok(new { interests });
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate([FromBody] GenerateItineraryRequest request, CancellationToken ct)
    {
        var command = new GenerateItineraryCommand
        {
            CityName = request.CityName,
            TotalBudget = request.TotalBudget,
            CurrencyCode = request.CurrencyCode,
            DurationDays = request.DurationDays,
            TripStartDate = request.TripStartDate,
            Interests = request.Interests,
            IncludeRestaurants = request.IncludeRestaurants,
            IncludeAccommodations = request.IncludeAccommodations,
            CuisinePreferences = request.CuisinePreferences
        };

        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Status = 400
            });

        var userId = GetUserId();
        var result = await _generationService.GenerateAsync(new ItineraryGenerationRequest(
            userId, request.CityName, request.TotalBudget, request.CurrencyCode,
            request.DurationDays, request.TripStartDate, request.Interests,
            request.IncludeRestaurants, request.IncludeAccommodations, request.CuisinePreferences), ct);

        if (!result.Success)
        {
            return result.Error?.Contains("could not be found") == true
                ? NotFound(new ProblemDetails { Title = "Not Found", Detail = result.Error, Status = 404 })
                : BadRequest(new ProblemDetails { Title = "Bad Request", Detail = result.Error, Status = 400 });
        }

        return Ok(MapToResponse(result.Itinerary, result.Notices));
    }

    [HttpPost("{id:guid}/save")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(Guid id, CancellationToken ct)
    {
        var itinerary = await _itineraryRepo.GetByIdAsync(id, ct);
        if (itinerary is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Itinerary not found.", Status = 404 });

        if (itinerary.UserId != GetUserId())
            return StatusCode(403, new ProblemDetails { Title = "Forbidden", Detail = "Cannot access this itinerary.", Status = 403 });

        itinerary.Status = "Saved";
        await _itineraryRepo.UpdateAsync(itinerary, ct);

        return Ok(new { itineraryId = id, status = "Saved", message = "Itinerary saved successfully." });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ItinerarySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var userId = GetUserId();

        var items = await _itineraryRepo.GetByUserIdAsync(userId, page, pageSize, ct);
        var totalCount = await _itineraryRepo.GetCountByUserIdAsync(userId, ct);

        return Ok(new PaginatedResponse<ItinerarySummaryResponse>
        {
            Items = items.Select(i => new ItinerarySummaryResponse
            {
                ItineraryId = i.Id,
                CityName = i.CityName,
                DurationDays = i.DurationDays,
                TotalBudget = i.TotalBudget,
                CurrencyCode = i.CurrencyCode,
                TripStartDate = i.TripStartDate,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var itinerary = await _itineraryRepo.GetByIdWithDetailsAsync(id, ct);
        if (itinerary is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Itinerary not found.", Status = 404 });

        if (itinerary.UserId != GetUserId())
            return StatusCode(403, new ProblemDetails { Title = "Forbidden", Detail = "Cannot access this itinerary.", Status = 403 });

        return Ok(MapToResponse(itinerary, []));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var itinerary = await _itineraryRepo.GetByIdAsync(id, ct);
        if (itinerary is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Itinerary not found.", Status = 404 });

        if (itinerary.UserId != GetUserId())
            return StatusCode(403, new ProblemDetails { Title = "Forbidden", Detail = "Cannot access this itinerary.", Status = 403 });

        await _itineraryRepo.DeleteAsync(id, ct);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }

    private static ItineraryResponse MapToResponse(Domain.Entities.Itinerary itinerary, List<string> notices)
    {
        return new ItineraryResponse
        {
            ItineraryId = itinerary.Id,
            CityName = itinerary.CityName,
            CountryName = itinerary.CountryName,
            Coordinates = new CoordinatesDto { Latitude = itinerary.Latitude, Longitude = itinerary.Longitude },
            TotalBudget = itinerary.TotalBudget,
            CurrencyCode = itinerary.CurrencyCode,
            DurationDays = itinerary.DurationDays,
            TripStartDate = itinerary.TripStartDate,
            Status = itinerary.Status,
            CreatedAt = itinerary.CreatedAt,
            Notices = notices,
            CostBreakdown = itinerary.CostBreakdown is not null
                ? new CostBreakdownResponse
                {
                    TotalActivitiesCost = itinerary.CostBreakdown.TotalActivitiesCost,
                    TotalDiningCost = itinerary.CostBreakdown.TotalDiningCost,
                    TotalTransportCost = itinerary.CostBreakdown.TotalTransportCost,
                    GrandTotal = itinerary.CostBreakdown.GrandTotal,
                    RemainingBudget = itinerary.CostBreakdown.RemainingBudget,
                    CurrencyCode = itinerary.CostBreakdown.CurrencyCode
                }
                : null,
            DayPlans = itinerary.DayPlans.OrderBy(dp => dp.DayNumber).Select(dp => new DayPlanResponse
            {
                DayNumber = dp.DayNumber,
                Date = dp.Date,
                DailyCostTotal = dp.DailyCostTotal,
                Weather = dp.WeatherCode.HasValue
                    ? new WeatherResponse
                    {
                        Summary = dp.WeatherSummary,
                        WeatherCode = dp.WeatherCode.Value,
                        MaxTemperatureC = dp.MaxTemperatureC ?? 0,
                        MinTemperatureC = dp.MinTemperatureC ?? 0,
                        PrecipitationMm = dp.PrecipitationMm ?? 0
                    }
                    : null,
                Activities = dp.Activities.OrderBy(a => a.OrderIndex).Select(a => new ActivitySlotResponse
                {
                    OrderIndex = a.OrderIndex,
                    StartTime = a.StartTime.ToString("HH:mm"),
                    EndTime = a.EndTime.ToString("HH:mm"),
                    PlaceName = a.PlaceName,
                    PlaceAddress = a.PlaceAddress,
                    Coordinates = new CoordinatesDto { Latitude = a.PlaceLatitude, Longitude = a.PlaceLongitude },
                    Category = a.Category,
                    IsIndoor = a.IsIndoor,
                    EstimatedCost = a.EstimatedCostUser,
                    VisitDurationMinutes = a.VisitDurationMinutes,
                    TravelTimeFromPrevMinutes = a.TravelTimeFromPrevMinutes,
                    TravelDistanceFromPrevKm = a.TravelDistanceFromPrevKm,
                    TransportMode = a.TransportMode
                }).ToList(),
                Restaurants = dp.Restaurants.Select(r => new RestaurantResponse
                {
                    MealSlot = r.MealSlot,
                    Name = r.Name,
                    CuisineType = r.CuisineType,
                    Coordinates = new CoordinatesDto { Latitude = r.Latitude, Longitude = r.Longitude },
                    DistanceFromActivityKm = r.DistanceFromActivityKm,
                    EstimatedMealCost = r.EstimatedMealCost,
                    MealTime = r.MealTime?.ToString("HH:mm")
                }).ToList()
            }).ToList()
        };
    }
}
