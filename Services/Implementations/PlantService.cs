using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class PlantService : IPlantService
{
    private readonly ApplicationDbContext _dbContext;

    public PlantService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var query = _dbContext.Plants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        query = (sortBy?.Trim().ToLowerInvariant(), isDescending) switch
        {
            ("name", true) => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            ("name", false) => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            (_, true) => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            (_, false) => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PlantDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return new PagedResultDto<PlantDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PlantDto> GetByIdAsync(int id)
    {
        var plant = await _dbContext.Plants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Plant with id '{id}' was not found.");

        return MapToDto(plant);
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

        return MapToDto(plant);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var plant = await _dbContext.Plants.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Plant with id '{id}' was not found.");

        var now = DateTime.UtcNow;
        plant.IsActive = false;
        plant.DeletedAt = now;
        plant.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();
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
