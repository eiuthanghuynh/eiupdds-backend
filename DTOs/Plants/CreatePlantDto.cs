using System.ComponentModel.DataAnnotations;

namespace eiupdds_backend.DTOs.Plants;

public class CreatePlantDto
{
    [Required(ErrorMessage = "Plant name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Plant name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;
}
