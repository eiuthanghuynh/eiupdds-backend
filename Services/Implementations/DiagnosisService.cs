using System.Text.Json;
using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.DTOs.Roboflow;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiupdds_backend.Services.Implementations;

/// <summary>
/// Coordinates plant disease diagnosis by uploading images, performing AI inference, and persisting prediction history.
/// </summary>
public class DiagnosisService : IDiagnosisService
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IRoboflowService _roboflowService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DiagnosisService> _logger;

    public DiagnosisService(
        IFileUploadService fileUploadService,
        IRoboflowService roboflowService,
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<DiagnosisService> logger)
    {
        _fileUploadService = fileUploadService;
        _roboflowService = roboflowService;
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PredictionResponseDto> PredictAsync(IFormFile file, string userId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(file));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty.", nameof(userId));
        }

        var relativeUrl = await _fileUploadService.UploadImageAsync(file);

        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var physicalPath = Path.Combine(webRoot, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            var modelOutput = await _roboflowService.DetectDiseaseAsync(physicalPath);

            var predictions = modelOutput?.Predictions ?? new List<RoboflowPredictionDetail>();

            var detectedLabels = predictions
                .Select(p => p.ClassName)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .SelectMany(c => new[] { c, c.ToLowerInvariant(), c.ToUpperInvariant() })
                .Distinct()
                .ToList();

            var detectedClassIds = predictions
                .Select(p => p.ClassId)
                .Distinct()
                .ToList();

            var diseases = detectedLabels.Count > 0 || detectedClassIds.Count > 0
                ? await _context.Diseases
                    .Where(d => detectedLabels.Contains(d.RoboflowLabel) || detectedClassIds.Contains(d.RoboflowClassId))
                    .ToListAsync()
                : new List<Disease>();

            int? matchedPlantId = null;
            var predictionItems = new List<PredictionItem>();

            foreach (var item in predictions)
            {
                var matchedDisease = diseases.FirstOrDefault(d =>
                    (!string.IsNullOrWhiteSpace(d.RoboflowLabel) && string.Equals(d.RoboflowLabel, item.ClassName, StringComparison.OrdinalIgnoreCase)))
                    ?? diseases.FirstOrDefault(d => d.RoboflowClassId == item.ClassId);

                if (matchedPlantId == null && matchedDisease != null)
                {
                    matchedPlantId = matchedDisease.PlantId;
                }

                predictionItems.Add(new PredictionItem
                {
                    DetectedClass = item.ClassName,
                    ClassId = item.ClassId,
                    Confidence = item.Confidence,
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    DetectionId = item.DetectionId,
                    DiseaseId = matchedDisease?.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var history = new PredictionHistory
            {
                UserId = userId,
                PlantId = matchedPlantId,
                ImageUrl = relativeUrl,
                ImageWidth = modelOutput?.Image?.Width ?? 0,
                ImageHeight = modelOutput?.Image?.Height ?? 0,
                RawResponse = JsonSerializer.Serialize(modelOutput),
                CreatedAt = DateTime.UtcNow,
                PredictionItems = predictionItems
            };

            _context.PredictionHistories.Add(history);
            await _context.SaveChangesAsync();

            return new PredictionResponseDto
            {
                Id = history.Id,
                ImageUrl = history.ImageUrl,
                Predictions = history.PredictionItems.Select(p => new PredictionItemDto
                {
                    Class = p.DetectedClass,
                    Confidence = p.Confidence,
                    X = p.X,
                    Y = p.Y,
                    Width = p.Width,
                    Height = p.Height
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _fileUploadService.DeleteImage(relativeUrl);
            _logger.LogError(ex, "Failed to complete disease diagnosis for user {UserId}. Cleaned up uploaded file at {RelativeUrl}.", userId, relativeUrl);
            throw;
        }
    }
}
