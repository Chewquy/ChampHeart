using ChampHeart.API.Data.Repositories.Interfaces;
using ChampHeart.API.DTOs;
using ChampHeart.API.Models;
using ChampHeart.API.Services.Interfaces;

namespace ChampHeart.API.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _repository;

    private static readonly TimeZoneInfo ClinicTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

    public ResourceService(IResourceRepository repository)
    {
        _repository = repository;
    }

    // Retrieve active resources with optional filters
    public async Task<List<ResourceResponseDTO>> GetResourcesAsync(
        string? search = null,
        string? category = null,
        DateOnly? date = null)
    {
        var resources = await _repository.GetAllAsync(search, category);

        if (date.HasValue)
        {
            var openingTime = TimeZoneInfo.ConvertTimeToUtc(date.Value.ToDateTime(new TimeOnly(9, 0)),ClinicTimeZone);

            var closingTime = TimeZoneInfo.ConvertTimeToUtc(date.Value.ToDateTime(new TimeOnly(17, 0)),ClinicTimeZone);

            var bookings =
                await _repository.GetApprovedBookingsAsync(openingTime, closingTime);

            resources = resources
                .Where(resource => HasAvailableSlot(
                    resource.Id,
                    openingTime,
                    closingTime,
                    bookings))
                .ToList();
        }

        return resources.Select(MapToDTO).ToList();
    }

    // Retrieve a specific active resource
    public async Task<ResourceResponseDTO> GetResourceByIdAsync(
        int id)
    {
        var resource = await _repository.GetByIdAsync(id);

        if (resource == null || !resource.IsActive)
        {
            throw new KeyNotFoundException("Resource not found or unavailable.");
        }

        return MapToDTO(resource);
    }

    // Create a new resource
    public async Task<ResourceResponseDTO> CreateResourceAsync(
        CreateResourceDTO request)
    {
        var resource = new Resource
        {
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            Description = request.Description?.Trim()
                ?? string.Empty,
            Capacity = request.Capacity,
            IsActive = true
        };

        var createdResource = await _repository.CreateAsync(resource);

        return MapToDTO(createdResource);
    }

    // Update an existing resource
    public async Task<ResourceResponseDTO> UpdateResourceAsync(
        int id,
        UpdateResourceDTO request)
    {
        var resource = await _repository.GetByIdAsync(id);

        if (resource == null)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        if (!request.IsActive.HasValue)
        {
            throw new ArgumentException("Resource active status is required.");
        }

        resource.Name = request.Name.Trim();
        resource.Category = request.Category.Trim();
        resource.Description = request.Description?.Trim()?? string.Empty;
        resource.Capacity = request.Capacity;
        resource.IsActive = request.IsActive.Value;

        await _repository.UpdateAsync(resource);

        return MapToDTO(resource);
    }

    // Soft-delete a resource
    public async Task<bool> DeleteResourceAsync(int id)
    {
        var resource = await _repository.GetByIdAsync(id);

        if (resource == null)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        resource.IsActive = false;

        await _repository.UpdateAsync(resource);

        return true;
    }

    // Retrieve all resources for administrators
    public async Task<List<ResourceResponseDTO>>
        GetAdminResourcesAsync()
    {
        var resources = await _repository.GetAllAsync(activeOnly: false);

        return resources.Select(MapToDTO).ToList();
    }

    // Determine whether a resource has a free appointment slot
    private static bool HasAvailableSlot(
        int resourceId,
        DateTime openingTime,
        DateTime closingTime,
        List<Booking> bookings)
    {
        var resourceBookings = bookings
            .Where(b => b.ResourceId == resourceId)
            .ToList();

        var currentTime = DateTime.UtcNow;

        for (var slotStart = openingTime;
             slotStart.AddMinutes(30) <= closingTime;
             slotStart = slotStart.AddMinutes(30))
        {
            if (slotStart <= currentTime)
                continue;

            var slotEnd = slotStart.AddMinutes(30);

            bool hasConflict = resourceBookings.Any(b =>
                b.StartTime < slotEnd &&
                slotStart < b.EndTime);

            if (!hasConflict)
                return true;
        }

        return false;
    }

    // Convert a Resource entity into a response DTO
    private static ResourceResponseDTO MapToDTO(
        Resource resource)
    {
        return new ResourceResponseDTO
        {
            Id = resource.Id,
            Name = resource.Name,
            Category = resource.Category,
            Description = resource.Description,
            Capacity = resource.Capacity,
            IsActive = resource.IsActive
        };
    }
}