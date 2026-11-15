namespace eiupdds_backend.DTOs.Diagnosis;

/// <summary>
/// Prediction response returned to the client after disease diagnosis.
/// </summary>
public class PredictionResponseDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string? PlantName { get; set; }
    public List<PredictionItemDto> Predictions { get; set; } = new();
}