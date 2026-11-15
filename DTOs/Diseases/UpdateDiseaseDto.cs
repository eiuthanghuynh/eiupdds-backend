using System.ComponentModel.DataAnnotations;

namespace eiupdds_backend.DTOs.Diseases;

public class UpdateDiseaseDto
{
    [Required(ErrorMessage = "PlantId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "PlantId must be a positive number.")]
    public int PlantId { get; set; }

    [Required(ErrorMessage = "Disease name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Disease name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Roboflow class id is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Roboflow class id cannot be negative.")]
    public int RoboflowClassId { get; set; }

    [Required(ErrorMessage = "Roboflow label is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Roboflow label must be between 1 and 200 characters.")]
    public string RoboflowLabel { get; set; } = string.Empty;
}
