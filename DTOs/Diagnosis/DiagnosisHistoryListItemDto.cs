namespace eiupdds_backend.DTOs.Diagnosis;

public class DiagnosisHistoryListItemDto
{
    public int Id { get; set; }
    public int? PlantId { get; set; }
    public string? PlantName { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int DetectionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
