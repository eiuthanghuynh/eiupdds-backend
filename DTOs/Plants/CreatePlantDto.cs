using System.ComponentModel.DataAnnotations;
namespace eiupdds_backend.DTOs.Plants;

public class CreatePlantDto
{
    [Required(ErrorMessage = "Tên cây không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên cây không được vượt quá 150 ký tự")]
    public string Name { get; set; } = string.Empty;
}
