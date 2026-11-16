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
    public async Task<ActionResult<PagedResult<PlantResponseDto>>> GetAll([FromQuery]
PlantQueryParameters query)
    {
        var result = await _plantService.GetAllPlantsAsync(query);
        return Ok(result);
    }
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PlantResponseDto>> GetById(int id)
    {
        var plant = await _plantService.GetPlantByIdAsync(id);
        if (plant == null) return NotFound(new { message = $"Không tìm thấy cây với ID = {id}" });
        return Ok(plant);
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlantResponseDto>> Create([FromBody] CreatePlantDto dto)
    {
        var created = await _plantService.CreatePlantAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlantResponseDto>> Update(int id, [FromBody] UpdatePlantDto dto)
    {
        var updated = await _plantService.UpdatePlantAsync(id, dto);
        if (updated == null) return NotFound(new { message = $"Không tìm thấy cây với ID = {id}" });
        return Ok(updated);
    }
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var success = await _plantService.SoftDeletePlantAsync(id);
        if (!success) return NotFound(new { message = $"Không tìm thấy cây với ID = {id}" });
        return NoContent();
    }
}
