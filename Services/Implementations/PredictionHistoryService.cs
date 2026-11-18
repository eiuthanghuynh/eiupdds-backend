using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class PredictionHistoryService : IPredictionHistoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileUploadService _fileUploadService;

    public PredictionHistoryService(ApplicationDbContext dbContext, IFileUploadService fileUploadService)
    {
        _dbContext = dbContext;
        _fileUploadService = fileUploadService;
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

        var query = _dbContext.PredictionHistories
            .AsNoTracking()
            .Include(h => h.Plant)
            .Include(h => h.PredictionItems)
            .Where(h => h.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            if (toDate.Value.TimeOfDay == TimeSpan.Zero)
            {
                var exclusiveEnd = toDate.Value.Date.AddDays(1);
                query = query.Where(h => h.CreatedAt < exclusiveEnd);
            }
            else
            {
                query = query.Where(h => h.CreatedAt <= toDate.Value);
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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
        var history = await _dbContext.PredictionHistories
            .Include(h => h.Plant)
            .Include(h => h.PredictionItems)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException($"Diagnosis history with id '{id}' was not found.");

        return MapToDetail(history);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var history = await _dbContext.PredictionHistories
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new KeyNotFoundException($"Diagnosis history with id '{id}' was not found.");

        var imageUrl = history.ImageUrl;
        _dbContext.PredictionHistories.Remove(history);
        await _dbContext.SaveChangesAsync();

        _fileUploadService.DeleteImage(imageUrl);
        _fileUploadService.DeleteImage(FileUploadService.GetAnnotatedRelativeUrl(imageUrl));
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
            AnnotatedImageUrl = string.IsNullOrWhiteSpace(history.ImageUrl)
                ? null
                : FileUploadService.GetAnnotatedRelativeUrl(history.ImageUrl),
            ImageWidth = history.ImageWidth,
            ImageHeight = history.ImageHeight,
            RawResponse = history.RawResponse,
            CreatedAt = history.CreatedAt,
            Predictions = history.PredictionItems
                .OrderByDescending(i => i.Confidence)
                .Select(i => new PredictionItemDto
                {
                    Id = i.Id,
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
