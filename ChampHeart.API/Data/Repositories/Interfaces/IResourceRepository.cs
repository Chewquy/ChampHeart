using ChampHeart.API.Models;

namespace ChampHeart.API.Data.Repositories.Interfaces;

public interface IResourceRepository
{
    Task<List<Resource>> GetAllAsync(
        string? search = null,
        string? category = null,
        bool activeOnly = true);

    Task<Resource?> GetByIdAsync(int id);

    Task<Resource> CreateAsync(Resource resource);

    Task UpdateAsync(Resource resource);

    Task<List<Booking>> GetApprovedBookingsAsync(DateTime start,DateTime end);
}