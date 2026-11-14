using eiupdds_backend.DTOs.Diagnosis;
using Microsoft.AspNetCore.Http;

namespace eiupdds_backend.Services.Interfaces;

/// <summary>
/// Service contract for diagnosing plant diseases from uploaded images.
/// </summary>
public interface IDiagnosisService
{
    /// <summary>
    /// Processes an uploaded image file, executes AI inference, and saves prediction history.
    /// </summary>
    /// <param name="file">Uploaded image file.</param>
    /// <param name="userId">Unique identifier of the authenticated user.</param>
    /// <returns>Minimal prediction response containing image URL and detected classes.</returns>
    Task<PredictionResponseDto> PredictAsync(IFormFile file, string userId);
}
