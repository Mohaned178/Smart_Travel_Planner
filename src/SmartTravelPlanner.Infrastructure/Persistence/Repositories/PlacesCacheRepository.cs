using Microsoft.EntityFrameworkCore;
using SmartTravelPlanner.Domain.Entities;
using SmartTravelPlanner.Domain.Interfaces;

namespace SmartTravelPlanner.Infrastructure.Persistence.Repositories;

public class PlacesCacheRepository : IPlacesCacheRepository
{
    private readonly AppDbContext _db;
    private const int StaleDays = 7;

    public PlacesCacheRepository(AppDbContext db) => _db = db;

    public async Task<Place?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        var place = await _db.Places.FindAsync([externalId], ct);
        if (place is null) return null;

        if (DateTime.UtcNow - place.CachedAt > TimeSpan.FromDays(StaleDays))
            return null;

        return place;
    }

    public async Task<IReadOnlyList<Place>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-StaleDays);
        var ids = externalIds.ToList();
        return await _db.Places
            .Where(p => ids.Contains(p.ExternalId) && p.CachedAt >= cutoff)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(Place place, CancellationToken ct = default)
    {
        var existing = await _db.Places.FindAsync([place.ExternalId], ct);
        if (existing is not null)
        {
            _db.Entry(existing).CurrentValues.SetValues(place);
            existing.CachedAt = DateTime.UtcNow;
        }
        else
        {
            place.CachedAt = DateTime.UtcNow;
            await _db.Places.AddAsync(place, ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}
