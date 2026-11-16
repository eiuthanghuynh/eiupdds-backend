using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Numerics;
namespace eiupdds_backend.Services.Implementations;

public class PlantService : IPlantService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string PlantListCacheKey = "plants_catalog_list";
    public PlantService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }
    public async Task<PagedResult<PlantResponseDto>> GetAllPlantsAsync(PlantQueryParameters query)
    {
        // 1. Khởi tạo query từ DB (Global Query Filter đã tự lọc IsActive == true)
        var plantQuery = _context.Plants.AsNoTracking();
        // 2. Tìm kiếm theo tên nếu có
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            plantQuery = plantQuery.Where(p => p.Name.Contains(query.SearchTerm.Trim()));
        }
        // 3. Sắp xếp
        plantQuery = query.SortBy?.ToLower() switch
        {
            "name" => query.IsDescending ? plantQuery.OrderByDescending(p => p.Name) :
            plantQuery.OrderBy(p => p.Name),
            _ => query.IsDescending ? plantQuery.OrderByDescending(p => p.CreatedAt) :
            plantQuery.OrderBy(p => p.CreatedAt)
        };
        var totalCount = await plantQuery.CountAsync();
        // 4. Phân trang
        var items = await plantQuery
        .Skip((query.PageNumber - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(p => new PlantResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        })
        .ToListAsync();
        return new PagedResult<PlantResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
    public async Task<PlantResponseDto?> GetPlantByIdAsync(int id)
    {
        var plant = await _context.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (plant == null) return null;
        return new PlantResponseDto
        {
            Id = plant.Id,
            Name = plant.Name,
            IsActive = plant.IsActive,
            CreatedAt = plant.CreatedAt,
            UpdatedAt = plant.UpdatedAt
        };
    }
    public async Task<PlantResponseDto> CreatePlantAsync(CreatePlantDto dto)
    {
        var plant = new Plant
        {
            Name = dto.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plants.Add(plant);
        await _context.SaveChangesAsync();
        // Xóa cache để lần sau lấy dữ liệu mới
        _cache.Remove(PlantListCacheKey);
        return new PlantResponseDto
        {
            Id = plant.Id,
            Name = plant.Name,
            IsActive = plant.IsActive,
            CreatedAt = plant.CreatedAt
        };
    }
    public async Task<PlantResponseDto?> UpdatePlantAsync(int id, UpdatePlantDto dto)
    {
        var plant = await _context.Plants.FirstOrDefaultAsync(p => p.Id == id);
        if (plant == null) return null;
        plant.Name = dto.Name.Trim();
        plant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _cache.Remove(PlantListCacheKey);
        return new PlantResponseDto
        {
            Id = plant.Id,
            Name = plant.Name,
            IsActive = plant.IsActive,
            CreatedAt = plant.CreatedAt,
            UpdatedAt = plant.UpdatedAt
        };
    }
    public async Task<bool> SoftDeletePlantAsync(int id)
    {
        var plant = await _context.Plants.FirstOrDefaultAsync(p => p.Id == id);
        if (plant == null) return false;
        plant.IsActive = false;
        plant.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _cache.Remove(PlantListCacheKey);
        return true;
    }
}