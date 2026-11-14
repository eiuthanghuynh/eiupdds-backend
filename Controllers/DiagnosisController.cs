using System.Security.Claims;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eiupdds_backend.Controllers;

/// <summary>
/// Endpoints for handling automated plant leaf disease diagnosis.
/// </summary>
[Authorize]
public class DiagnosisController : BaseApiController
{
    private readonly IDiagnosisService _diagnosisService;

    public DiagnosisController(IDiagnosisService diagnosisService)
    {
        _diagnosisService = diagnosisService;
    }

    /// <summary>
    /// Analyzes an uploaded plant leaf image and returns disease predictions.
    /// </summary>
    /// <param name="file">Uploaded image of a plant leaf.</param>
    /// <returns>Prediction results including bounding box locations and identified disease classes.</returns>
    [HttpPost("predict")]
    public async Task<ActionResult<PredictionResponseDto>> Predict(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var response = await _diagnosisService.PredictAsync(file, userId);
        return Ok(response);
    }
}
