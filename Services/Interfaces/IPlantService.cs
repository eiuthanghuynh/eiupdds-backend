using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Plants;

namespace eiupdds_backend.Services.Interfaces;

public interface IPlantService
{
    Task<PagedResultDto<PlantDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? sortBy,
        bool isDescending);

    Task<PlantDto> GetByIdAsync(int id);
    Task<PlantDto> CreateAsync(CreatePlantDto request);
    Task<PlantDto> UpdateAsync(int id, UpdatePlantDto request);
    Task SoftDeleteAsync(int id);
}
