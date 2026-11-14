using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Diseases;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class DiseaseService : IDiseaseService
{
    private readonly ApplicationDbContext _dbContext;

    public DiseaseService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DiseaseDto>> GetAllAsync(int? plantId, string? search)
    {
        var query = _dbContext.Diseases
            .AsNoTracking()
            .Include(d => d.Plant)
            .AsQueryable();

        if (plantId.HasValue)
        {
            query = query.Where(d => d.PlantId == plantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term) || d.RoboflowLabel.Contains(term));
        }

        var diseases = await query
            .OrderBy(d => d.Name)
            .ToListAsync();

        return diseases.Select(MapToDto).ToList();
    }

    public async Task<DiseaseDto> GetByIdAsync(int id)
    {
        var disease = await _dbContext.Diseases
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Disease with id '{id}' was not found.");

        return MapToDto(disease);
    }

    public async Task<DiseaseDto> CreateAsync(CreateDiseaseDto request)
    {
        await EnsurePlantExistsAsync(request.PlantId);

        var disease = new Disease
        {
            PlantId = request.PlantId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            RoboflowClassId = request.RoboflowClassId,
            RoboflowLabel = request.RoboflowLabel.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Diseases.Add(disease);
        await _dbContext.SaveChangesAsync();

        var withPlant = await _dbContext.Diseases
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.Id == disease.Id)
            ?? disease;

        return MapToDto(withPlant);
    }

    public async Task<DiseaseDto> UpdateAsync(int id, UpdateDiseaseDto request)
    {
        var disease = await _dbContext.Diseases
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Disease with id '{id}' was not found.");

        await EnsurePlantExistsAsync(request.PlantId);

        disease.PlantId = request.PlantId;
        disease.Name = request.Name.Trim();
        disease.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        disease.RoboflowClassId = request.RoboflowClassId;
        disease.RoboflowLabel = request.RoboflowLabel.Trim();
        disease.UpdatedAt = DateTime.UtcNow;

        _dbContext.Diseases.Update(disease);
        await _dbContext.SaveChangesAsync();

        return MapToDto(disease);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var disease = await _dbContext.Diseases
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Disease with id '{id}' was not found.");

        disease.IsActive = false;
        disease.DeletedAt = DateTime.UtcNow;
        disease.UpdatedAt = DateTime.UtcNow;

        _dbContext.Diseases.Update(disease);
        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsurePlantExistsAsync(int plantId)
    {
        var plantExists = await _dbContext.Plants.AnyAsync(p => p.Id == plantId);
        if (!plantExists)
        {
            throw new KeyNotFoundException($"Plant with id '{plantId}' was not found.");
        }
    }

    private static DiseaseDto MapToDto(Disease disease)
    {
        return new DiseaseDto
        {
            Id = disease.Id,
            PlantId = disease.PlantId,
            PlantName = disease.Plant?.Name ?? string.Empty,
            Name = disease.Name,
            Description = disease.Description,
            RoboflowClassId = disease.RoboflowClassId,
            RoboflowLabel = disease.RoboflowLabel,
            IsActive = disease.IsActive,
            CreatedAt = disease.CreatedAt,
            UpdatedAt = disease.UpdatedAt
        };
    }
}
