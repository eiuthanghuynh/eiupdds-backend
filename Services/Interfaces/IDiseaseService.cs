using eiupdds_backend.DTOs.Diseases;

namespace eiupdds_backend.Services.Interfaces;

public interface IDiseaseService
{
    Task<IReadOnlyList<DiseaseDto>> GetAllAsync(int? plantId, string? search);
    Task<DiseaseDto> GetByIdAsync(int id);
    Task<DiseaseDto> CreateAsync(CreateDiseaseDto request);
    Task<DiseaseDto> UpdateAsync(int id, UpdateDiseaseDto request);
    Task SoftDeleteAsync(int id);
}
