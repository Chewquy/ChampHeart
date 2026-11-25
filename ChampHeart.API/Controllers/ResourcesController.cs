using ChampHeart.API.DTOs;
using ChampHeart.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize(Roles = "Member,Admin")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public ResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    // GET: api/resources
    [HttpGet]
    public async Task<ActionResult<List<ResourceResponseDTO>>>
        GetResources(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] DateOnly? date = null)
    {
        var resources = await _resourceService.GetResourcesAsync(search, category, date);

        return Ok(resources);
    }

    // GET: api/resources/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResourceResponseDTO>>
        GetResourceById(int id)
    {
        try
        {
            var resource = await _resourceService.GetResourceByIdAsync(id);

            return Ok(resource);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}