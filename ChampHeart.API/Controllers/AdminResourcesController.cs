using ChampHeart.API.DTOs;
using ChampHeart.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers;

[ApiController]
[Route("api/admin/resources")]
[Authorize(Roles = "Admin")]
public class AdminResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public AdminResourcesController(
        IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    // GET: api/admin/resources
    [HttpGet]
    public async Task<ActionResult<List<ResourceResponseDTO>>>
        GetAllResources()
    {
        var resources = await _resourceService.GetAdminResourcesAsync();

        return Ok(resources);
    }

    // POST: api/admin/resources
    [HttpPost]
    public async Task<ActionResult<ResourceResponseDTO>>
        CreateResource([FromBody] CreateResourceDTO request)
    {
        var resource = await _resourceService.CreateResourceAsync(request);

        return CreatedAtAction(
            nameof(ResourcesController.GetResourceById),
            "Resources",
            new { id = resource.Id },
            resource);
    }

    // PUT: api/admin/resources/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResourceResponseDTO>>
        UpdateResource(int id,[FromBody] UpdateResourceDTO request)
    {
        try
        {
            var resource = await _resourceService.UpdateResourceAsync(id, request);

            return Ok(resource);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/admin/resources/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        try
        {
            await _resourceService.DeleteResourceAsync(id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}