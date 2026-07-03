using BookingManager.Api.Dtos;
using BookingManager.Api.Mapping;
using BookingManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingManager.Api.Controllers;

[ApiController]
[Route("api/resources")]
public class ResourcesController(IResourceService resourceService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ResourceResponse>>> GetAll()
    {
        var resources = await resourceService.GetAllAsync();
        return Ok(resources.Select(r => r.ToResponse()).ToList());
    }
}
