using eiupdds_backend.DTOs.Plants;
namespace eiupdds_backend.Services.Interfaces;

public interface IPlantService
{
    Task<PagedResult<PlantResponseDto>> GetAllPlantsAsync(PlantQueryParameters query);
    Task<PlantResponseDto?> GetPlantByIdAsync(int id);
    Task<PlantResponseDto> CreatePlantAsync(CreatePlantDto dto);
    Task<PlantResponseDto?> UpdatePlantAsync(int id, UpdatePlantDto dto);
    Task<bool> SoftDeletePlantAsync(int id);
}
