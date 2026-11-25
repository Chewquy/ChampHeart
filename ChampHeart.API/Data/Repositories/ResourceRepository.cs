using ChampHeart.API.Models;
using ChampHeart.API.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChampHeart.API.Data.Repositories;

public class ResourceRepository : IResourceRepository
{
    private readonly ApplicationDbContext _context;

    public ResourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Resource>> GetAllAsync(
        string? search = null,
        string? category = null,
        bool activeOnly = true)
    {
        var query = _context.Resources.AsNoTracking().AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(r => r.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();

            query = query.Where(r => r.Category == category);
        }

        return await query.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<Resource?> GetByIdAsync(int id)
    {
        return await _context.Resources.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Resource> CreateAsync(
        Resource resource)
    {
        _context.Resources.Add(resource);

        await _context.SaveChangesAsync();

        return resource;
    }

    public async Task UpdateAsync(Resource resource)
    {
        _context.Resources.Update(resource);

        await _context.SaveChangesAsync();
    }

    public async Task<List<Booking>> GetApprovedBookingsAsync(
        DateTime start,
        DateTime end)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.Status == BookingStatus.Approved &&
                b.StartTime < end &&
                start < b.EndTime)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }
}