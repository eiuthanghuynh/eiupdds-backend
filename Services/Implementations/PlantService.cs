using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diseases;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eiupdds_backend.Services.Implementations;

public class PlantService : IPlantService
{
    private const string PlantListCacheKey = "plants_catalog_all";
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public PlantService(ApplicationDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<PagedResultDto<PlantDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? sortBy,
        bool isDescending)
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

        var cachedPlants = await _cache.GetOrCreateAsync(PlantListCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _dbContext.Plants
                .AsNoTracking()
                .Select(p => new PlantDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();
        }) ?? [];

        IEnumerable<PlantDto> query = cachedPlants;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = ((sortBy?.Trim().ToLowerInvariant(), isDescending) switch
        {
            ("name", true) => query.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenByDescending(p => p.Id),
            ("name", false) => query.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id),
            (_, true) => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            (_, false) => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
        }).ToList();

        var items = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResultDto<PlantDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = ordered.Count
        };
    }

    public async Task<PlantDetailDto> GetByIdAsync(int id)
    {
        var plant = await _dbContext.Plants
            .AsNoTracking()
            .Include(p => p.Diseases)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Plant with id '{id}' was not found.");

        return new PlantDetailDto
        {
            Id = plant.Id,
            Name = plant.Name,
            IsActive = plant.IsActive,
            CreatedAt = plant.CreatedAt,
            UpdatedAt = plant.UpdatedAt,
            Diseases = plant.Diseases
                .OrderBy(d => d.Name)
                .Select(d => new DiseaseDto
                {
                    Id = d.Id,
                    PlantId = d.PlantId,
                    PlantName = plant.Name,
                    Name = d.Name,
                    Description = d.Description,
                    RoboflowClassId = d.RoboflowClassId,
                    RoboflowLabel = d.RoboflowLabel,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToList()
        };
    }

    public async Task<PlantDto> CreateAsync(CreatePlantDto request)
    {
        var trimmedName = request.Name.Trim();
        var normalizedName = trimmedName.ToLowerInvariant();

        var exists = await _dbContext.Plants
            .AnyAsync(p => p.Name.ToLower() == normalizedName);

        if (exists)
        {
            throw new ArgumentException($"Plant with name '{trimmedName}' already exists.");
        }

        var plant = new Plant
        {
            Name = trimmedName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Plants.Add(plant);
        await _dbContext.SaveChangesAsync();
        _cache.Remove(PlantListCacheKey);

        return MapToDto(plant);
    }

    public async Task<PlantDto> UpdateAsync(int id, UpdatePlantDto request)
    {
        var plant = await _dbContext.Plants.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Plant with id '{id}' was not found.");

        var trimmedName = request.Name.Trim();
        var normalizedName = trimmedName.ToLowerInvariant();

        var duplicateExists = await _dbContext.Plants
            .AnyAsync(p => p.Id != id && p.Name.ToLower() == normalizedName);

        if (duplicateExists)
        {
            throw new ArgumentException($"Plant with name '{trimmedName}' already exists.");
        }

        plant.Name = trimmedName;
        plant.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        _cache.Remove(PlantListCacheKey);

        return MapToDto(plant);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var plant = await _dbContext.Plants
            .Include(p => p.Diseases)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Plant with id '{id}' was not found.");

        var now = DateTime.UtcNow;
        plant.IsActive = false;
        plant.DeletedAt = now;
        plant.UpdatedAt = now;

        foreach (var disease in plant.Diseases)
        {
            disease.IsActive = false;
            disease.DeletedAt = now;
            disease.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        _cache.Remove(PlantListCacheKey);
    }

    private static PlantDto MapToDto(Plant plant)
    {
        return new PlantDto
        {
            Id = plant.Id,
            Name = plant.Name,
            IsActive = plant.IsActive,
            CreatedAt = plant.CreatedAt,
            UpdatedAt = plant.UpdatedAt
        };
    }
}
