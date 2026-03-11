using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartTravelPlanner.Domain.Entities;

namespace SmartTravelPlanner.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<ActivitySlot> ActivitySlots => Set<ActivitySlot>();
    public DbSet<CostBreakdown> CostBreakdowns => Set<CostBreakdown>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<RestaurantSuggestion> RestaurantSuggestions => Set<RestaurantSuggestion>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
        ConfigureInterest(builder);
        ConfigureItinerary(builder);
        ConfigureDayPlan(builder);
        ConfigureActivitySlot(builder);
        ConfigureCostBreakdown(builder);
        ConfigureRestaurantSuggestion(builder);
        ConfigurePlace(builder);
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureInterest(ModelBuilder builder)
    {
        builder.Entity<Interest>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(i => i.Name).IsUnique();
            e.Property(i => i.Category).IsRequired().HasMaxLength(50);
            e.Property(i => i.DisplayName).IsRequired().HasMaxLength(100);

            e.HasData(
                new Interest { Id = 1, Name = "museums", Category = "cultural", DisplayName = "Museums" },
                new Interest { Id = 2, Name = "parks", Category = "natural", DisplayName = "Parks & Gardens" },
                new Interest { Id = 3, Name = "food", Category = "food", DisplayName = "Food & Dining" },
                new Interest { Id = 4, Name = "nightlife", Category = "amusements", DisplayName = "Nightlife" },
                new Interest { Id = 5, Name = "shopping", Category = "shops", DisplayName = "Shopping" },
                new Interest { Id = 6, Name = "history", Category = "cultural", DisplayName = "History & Heritage" },
                new Interest { Id = 7, Name = "landmarks", Category = "cultural", DisplayName = "Landmarks" },
                new Interest { Id = 8, Name = "adventure", Category = "sport", DisplayName = "Adventure & Sports" },
                new Interest { Id = 9, Name = "beaches", Category = "natural", DisplayName = "Beaches" },
                new Interest { Id = 10, Name = "art", Category = "cultural", DisplayName = "Art & Galleries" }
            );
        });
    }

    private static void ConfigureItinerary(ModelBuilder builder)
    {
        builder.Entity<Itinerary>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.CityName).IsRequired().HasMaxLength(200);
            e.Property(i => i.CountryName).HasMaxLength(200);
            e.Property(i => i.Timezone).HasMaxLength(50);
            e.Property(i => i.TotalBudget).HasColumnType("decimal(18,2)");
            e.Property(i => i.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(i => i.Latitude).HasColumnType("decimal(10,7)");
            e.Property(i => i.Longitude).HasColumnType("decimal(10,7)");
            e.Property(i => i.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Draft");
            e.Property(i => i.CreatedAt).IsRequired();

            e.HasOne(i => i.User)
                .WithMany(u => u.Itineraries)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.CostBreakdown)
                .WithOne(cb => cb.Itinerary)
                .HasForeignKey<CostBreakdown>(cb => cb.ItineraryId);

            e.HasMany(i => i.DayPlans)
                .WithOne(dp => dp.Itinerary)
                .HasForeignKey(dp => dp.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => i.UserId);
            e.HasIndex(i => new { i.UserId, i.Status });
        });
    }

    private static void ConfigureDayPlan(ModelBuilder builder)
    {
        builder.Entity<DayPlan>(e =>
        {
            e.HasKey(dp => dp.Id);
            e.Property(dp => dp.WeatherSummary).HasMaxLength(200);
            e.Property(dp => dp.MaxTemperatureC).HasColumnType("decimal(5,2)");
            e.Property(dp => dp.MinTemperatureC).HasColumnType("decimal(5,2)");
            e.Property(dp => dp.PrecipitationMm).HasColumnType("decimal(7,2)");
            e.Property(dp => dp.DailyCostTotal).HasColumnType("decimal(18,2)");

            e.HasMany(dp => dp.Activities)
                .WithOne(a => a.DayPlan)
                .HasForeignKey(a => a.DayPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(dp => dp.Restaurants)
                .WithOne(r => r.DayPlan)
                .HasForeignKey(r => r.DayPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureActivitySlot(ModelBuilder builder)
    {
        builder.Entity<ActivitySlot>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.PlaceName).IsRequired().HasMaxLength(300);
            e.Property(a => a.PlaceAddress).HasMaxLength(500);
            e.Property(a => a.PlaceLatitude).HasColumnType("decimal(10,7)");
            e.Property(a => a.PlaceLongitude).HasColumnType("decimal(10,7)");
            e.Property(a => a.Category).IsRequired().HasMaxLength(50);
            e.Property(a => a.EstimatedCostLocal).HasColumnType("decimal(18,2)");
            e.Property(a => a.EstimatedCostUser).HasColumnType("decimal(18,2)");
            e.Property(a => a.TravelTimeFromPrevMinutes).HasColumnType("decimal(7,2)");
            e.Property(a => a.TravelDistanceFromPrevKm).HasColumnType("decimal(10,3)");
            e.Property(a => a.ExternalPlaceId).HasMaxLength(100);
        });
    }

    private static void ConfigureCostBreakdown(ModelBuilder builder)
    {
        builder.Entity<CostBreakdown>(e =>
        {
            e.HasKey(cb => cb.Id);
            e.Property(cb => cb.TotalActivitiesCost).HasColumnType("decimal(18,2)");
            e.Property(cb => cb.TotalDiningCost).HasColumnType("decimal(18,2)");
            e.Property(cb => cb.TotalTransportCost).HasColumnType("decimal(18,2)");
            e.Property(cb => cb.GrandTotal).HasColumnType("decimal(18,2)");
            e.Property(cb => cb.RemainingBudget).HasColumnType("decimal(18,2)");
            e.Property(cb => cb.CurrencyCode).IsRequired().HasMaxLength(3);

            e.HasIndex(cb => cb.ItineraryId).IsUnique();
        });
    }

    private static void ConfigureRestaurantSuggestion(ModelBuilder builder)
    {
        builder.Entity<RestaurantSuggestion>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.MealSlot).IsRequired().HasMaxLength(20);
            e.Property(r => r.Name).IsRequired().HasMaxLength(300);
            e.Property(r => r.CuisineType).HasMaxLength(100);
            e.Property(r => r.Latitude).HasColumnType("decimal(10,7)");
            e.Property(r => r.Longitude).HasColumnType("decimal(10,7)");
            e.Property(r => r.DistanceFromActivityKm).HasColumnType("decimal(10,3)");
            e.Property(r => r.EstimatedMealCost).HasColumnType("decimal(18,2)");
            e.Property(r => r.ExternalPlaceId).HasMaxLength(100);
        });
    }

    private static void ConfigurePlace(ModelBuilder builder)
    {
        builder.Entity<Place>(e =>
        {
            e.HasKey(p => p.ExternalId);
            e.Property(p => p.ExternalId).HasMaxLength(100);
            e.Property(p => p.Name).IsRequired().HasMaxLength(300);
            e.Property(p => p.Address).HasMaxLength(500);
            e.Property(p => p.Latitude).HasColumnType("decimal(10,7)");
            e.Property(p => p.Longitude).HasColumnType("decimal(10,7)");
            e.Property(p => p.Category).IsRequired().HasMaxLength(50);
            e.Property(p => p.EstimatedCost).HasColumnType("decimal(18,2)");
            e.Property(p => p.Rating).HasColumnType("decimal(3,1)");
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.CachedAt).IsRequired();
        });
    }
}
