using System.Security.Claims;
using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

/// <summary>
/// Endpoints for handling automated plant leaf disease diagnosis and history.
/// </summary>
[Authorize]
public class DiagnosisController : BaseApiController
{
    private readonly IDiagnosisService _diagnosisService;
    private readonly IPredictionHistoryService _predictionHistoryService;

    public DiagnosisController(
        IDiagnosisService diagnosisService,
        IPredictionHistoryService predictionHistoryService)
    {
        _diagnosisService = diagnosisService;
        _predictionHistoryService = predictionHistoryService;
    }

    /// <summary>
    /// Analyzes an uploaded plant leaf image and returns disease predictions.
    /// </summary>
    /// <param name="file">Uploaded image of a plant leaf.</param>
    /// <returns>Prediction results including bounding box locations and identified disease classes.</returns>
    [HttpPost("predict")]
    public async Task<ActionResult<PredictionResponseDto>> Predict(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var response = await _diagnosisService.PredictAsync(file, userId);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves paginated diagnosis history for the current authenticated user.
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<PagedResultDto<DiagnosisHistoryListItemDto>>> GetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
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

    /// <summary>
    /// Retrieves details of a specific diagnosis history record for the current user.
    /// </summary>
    [HttpGet("history/{id:int}")]
    public async Task<ActionResult<DiagnosisHistoryDetailDto>> GetHistoryById(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var history = await _predictionHistoryService.GetByIdAsync(id, userId);
        return Ok(history);
    }

    /// <summary>
    /// Deletes a specific diagnosis history record for the current user.
    /// </summary>
    [HttpDelete("history/{id:int}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
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
