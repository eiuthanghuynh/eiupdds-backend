using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.Models;
using eiupdds_backend.Repositories.Interfaces;
using eiupdds_backend.Services.Interfaces;

namespace eiupdds_backend.Services.Implementations;

public class PredictionHistoryService : IPredictionHistoryService
{
    private readonly IPredictionHistoryRepository _historyRepository;

    public PredictionHistoryService(IPredictionHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task<PagedResultDto<DiagnosisHistoryListItemDto>> GetHistoryAsync(
        string userId,
        int pageNumber,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var (items, totalCount) = await _historyRepository.GetByUserAsync(
            userId,
            pageNumber,
            pageSize,
            fromDate,
            toDate);

        return new PagedResultDto<DiagnosisHistoryListItemDto>
        {
            Items = items.Select(MapToListItem).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DiagnosisHistoryDetailDto> GetByIdAsync(int id, string userId)
    {
        var history = await _historyRepository.GetByIdForUserAsync(id, userId)
            ?? throw new KeyNotFoundException($"Diagnosis history with id '{id}' was not found.");

        return MapToDetail(history);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var history = await _historyRepository.GetByIdForUserAsync(id, userId)
            ?? throw new KeyNotFoundException($"Diagnosis history with id '{id}' was not found.");

        await _historyRepository.DeleteAsync(history);
    }

    private static DiagnosisHistoryListItemDto MapToListItem(PredictionHistory history)
    {
        return new DiagnosisHistoryListItemDto
        {
            Id = history.Id,
            PlantId = history.PlantId,
            PlantName = history.Plant?.Name,
            ImageUrl = history.ImageUrl,
            ImageWidth = history.ImageWidth,
            ImageHeight = history.ImageHeight,
            DetectionCount = history.PredictionItems.Count,
            CreatedAt = history.CreatedAt
        };
    }

    private static DiagnosisHistoryDetailDto MapToDetail(PredictionHistory history)
    {
        return new DiagnosisHistoryDetailDto
        {
            Id = history.Id,
            PlantId = history.PlantId,
            PlantName = history.Plant?.Name,
            ImageUrl = history.ImageUrl,
            ImageWidth = history.ImageWidth,
            ImageHeight = history.ImageHeight,
            RawResponse = history.RawResponse,
            CreatedAt = history.CreatedAt,
            Predictions = history.PredictionItems
                .OrderByDescending(i => i.Confidence)
                .Select(i => new PredictionItemDto
                {
                    Id = i.Id,
                    DiseaseId = i.DiseaseId,
                    DiseaseName = i.Disease?.Name,
                    DetectedClass = i.DetectedClass,
                    ClassId = i.ClassId,
                    Confidence = i.Confidence,
                    X = i.X,
                    Y = i.Y,
                    Width = i.Width,
                    Height = i.Height,
                    DetectionId = i.DetectionId
                })
                .ToList()
        };
    }
}
