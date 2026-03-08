using Microsoft.EntityFrameworkCore;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;

namespace SmartTravelPlanner.Infrastructure.Persistence.Repositories;

public class ItineraryRepository : IItineraryRepository
{
    private readonly AppDbContext _db;

    public ItineraryRepository(AppDbContext db) => _db = db;

    public async Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Itineraries.FindAsync([id], ct);

    public async Task<Itinerary?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _db.Itineraries
            .Include(i => i.DayPlans).ThenInclude(dp => dp.Activities)
            .Include(i => i.DayPlans).ThenInclude(dp => dp.Restaurants)
            .Include(i => i.CostBreakdown)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Itinerary>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _db.Itineraries
            .Where(i => i.UserId == userId && i.Status == "Saved")
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.Itineraries.CountAsync(i => i.UserId == userId && i.Status == "Saved", ct);

    public async Task AddAsync(Itinerary itinerary, CancellationToken ct = default)
    {
        await _db.Itineraries.AddAsync(itinerary, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Itinerary itinerary, CancellationToken ct = default)
    {
        _db.Itineraries.Update(itinerary);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var itinerary = await _db.Itineraries.FindAsync([id], ct);
        if (itinerary is not null)
        {
            _db.Itineraries.Remove(itinerary);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteStaleDraftsAsync(int expirationHours, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-expirationHours);
        var staleDrafts = await _db.Itineraries
            .Where(i => i.Status == "Draft" && i.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (staleDrafts.Count > 0)
        {
            _db.Itineraries.RemoveRange(staleDrafts);
            await _db.SaveChangesAsync(ct);
        }
    }
}
