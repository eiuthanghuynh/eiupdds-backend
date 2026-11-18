using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

public class PlantsController : BaseApiController
{
    private readonly IPlantService _plantService;

    public PlantsController(IPlantService plantService)
    {
        _plantService = plantService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<PlantDto>>> GetPlants(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isDescending = false)
    {
        var result = await _plantService.GetAllAsync(pageNumber, pageSize, search, sortBy, isDescending);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PlantDetailDto>> GetPlantById(int id)
    {
        var plant = await _plantService.GetByIdAsync(id);
        return Ok(plant);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlantDto>> CreatePlant([FromBody] CreatePlantDto request)
    {
        var created = await _plantService.CreateAsync(request);
        return CreatedAtAction(nameof(GetPlantById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlantDto>> UpdatePlant(int id, [FromBody] UpdatePlantDto request)
    {
        var updated = await _plantService.UpdateAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlant(int id)
    {
        await _plantService.SoftDeleteAsync(id);
        return Ok(new { message = $"Plant '{id}' has been deactivated successfully." });
    }
}
