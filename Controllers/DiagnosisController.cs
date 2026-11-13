using System.Security.Claims;
using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

[Authorize]
public class DiagnosisController : BaseApiController
{
    private readonly IPredictionHistoryService _predictionHistoryService;

    public DiagnosisController(IPredictionHistoryService predictionHistoryService)
    {
        _predictionHistoryService = predictionHistoryService;
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResultDto<DiagnosisHistoryListItemDto>>> GetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var history = await _predictionHistoryService.GetHistoryAsync(
            userId,
            pageNumber,
            pageSize,
            fromDate,
            toDate);

        return Ok(history);
    }

    [HttpGet("history/{id:int}")]
    public async Task<ActionResult<DiagnosisHistoryDetailDto>> GetHistoryById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var history = await _predictionHistoryService.GetByIdAsync(id, userId);
        return Ok(history);
    }

    [HttpDelete("history/{id:int}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _predictionHistoryService.DeleteAsync(id, userId);
        return Ok(new { message = $"Diagnosis history '{id}' has been deleted successfully." });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
