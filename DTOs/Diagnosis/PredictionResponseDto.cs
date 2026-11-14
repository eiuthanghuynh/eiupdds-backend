namespace eiupdds_backend.DTOs.Diagnosis;

/// <summary>
/// Minimal prediction response returned to the client after disease diagnosis.
/// </summary>
public class PredictionResponseDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public List<PredictionItemDto> Predictions { get; set; } = new();
}
