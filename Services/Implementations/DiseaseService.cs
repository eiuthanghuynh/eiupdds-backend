using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Diseases;
using eiupdds_backend.Models;
using eiupdds_backend.Repositories.Interfaces;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class DiseaseService : IDiseaseService
{
    private readonly IDiseaseRepository _diseaseRepository;
    private readonly ApplicationDbContext _dbContext;

    public DiseaseService(IDiseaseRepository diseaseRepository, ApplicationDbContext dbContext)
    {
        _diseaseRepository = diseaseRepository;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DiseaseDto>> GetAllAsync(int? plantId, string? search)
    {
        var diseases = await _diseaseRepository.GetActiveAsync(plantId, search);
        return diseases.Select(MapToDto).ToList();
    }

    public async Task<DiseaseDto> GetByIdAsync(int id)
    {
        var disease = await _diseaseRepository.GetByIdAsync(id)
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

        var created = await _diseaseRepository.AddAsync(disease);
        var withPlant = await _diseaseRepository.GetByIdAsync(created.Id)
            ?? created;

        return MapToDto(withPlant);
    }

    public async Task<DiseaseDto> UpdateAsync(int id, UpdateDiseaseDto request)
    {
        var disease = await _diseaseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Disease with id '{id}' was not found.");

        await EnsurePlantExistsAsync(request.PlantId);

        disease.PlantId = request.PlantId;
        disease.Name = request.Name.Trim();
        disease.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        disease.RoboflowClassId = request.RoboflowClassId;
        disease.RoboflowLabel = request.RoboflowLabel.Trim();
        disease.UpdatedAt = DateTime.UtcNow;

        await _diseaseRepository.UpdateAsync(disease);

        var updated = await _diseaseRepository.GetByIdAsync(id) ?? disease;
        return MapToDto(updated);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var disease = await _diseaseRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Disease with id '{id}' was not found.");

        disease.IsActive = false;
        disease.DeletedAt = DateTime.UtcNow;
        disease.UpdatedAt = DateTime.UtcNow;

        await _diseaseRepository.UpdateAsync(disease);
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
