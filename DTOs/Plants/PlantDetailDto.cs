using eiupdds_backend.DTOs.Diseases;

namespace eiupdds_backend.DTOs.Plants;

public class PlantDetailDto : PlantDto
{
    public List<DiseaseDto> Diseases { get; set; } = [];
}
