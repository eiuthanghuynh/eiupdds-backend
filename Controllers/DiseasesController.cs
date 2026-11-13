using eiupdds_backend.DTOs.Diseases;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

public class DiseasesController : BaseApiController
{
    private readonly IDiseaseService _diseaseService;

    public DiseasesController(IDiseaseService diseaseService)
    {
        _diseaseService = diseaseService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiseaseDto>>> GetDiseases(
        [FromQuery] int? plantId,
        [FromQuery] string? search)
    {
        var diseases = await _diseaseService.GetAllAsync(plantId, search);
        return Ok(diseases);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DiseaseDto>> GetDiseaseById(int id)
    {
        var disease = await _diseaseService.GetByIdAsync(id);
        return Ok(disease);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DiseaseDto>> CreateDisease([FromBody] CreateDiseaseDto request)
    {
        var created = await _diseaseService.CreateAsync(request);
        return CreatedAtAction(nameof(GetDiseaseById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DiseaseDto>> UpdateDisease(int id, [FromBody] UpdateDiseaseDto request)
    {
        var updated = await _diseaseService.UpdateAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDisease(int id)
    {
        await _diseaseService.SoftDeleteAsync(id);
        return Ok(new { message = $"Disease '{id}' has been deactivated successfully." });
    }
}
