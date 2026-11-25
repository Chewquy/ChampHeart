using ChampHeart.API.DTOs;

namespace ChampHeart.API.Services.Interfaces;

public interface IResourceService
{
    Task<List<ResourceResponseDTO>> GetResourcesAsync(
        string? search = null,
        string? category = null,
        DateOnly? date = null);

    Task<ResourceResponseDTO> GetResourceByIdAsync(int id);

    Task<ResourceResponseDTO> CreateResourceAsync(CreateResourceDTO request);

    Task<ResourceResponseDTO> UpdateResourceAsync(int id,UpdateResourceDTO request);

    Task<bool> DeleteResourceAsync(int id);

    Task<List<ResourceResponseDTO>> GetAdminResourcesAsync();
}