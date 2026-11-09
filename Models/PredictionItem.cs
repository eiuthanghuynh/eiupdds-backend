namespace eiupdds_backend.Models;

public class PredictionItem
{
    public int Id { get; set; }
    public int PredictionHistoryId { get; set; }
    public int? DiseaseId { get; set; }
    public string DetectedClass { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public double Confidence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string? DetectionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PredictionHistory PredictionHistory { get; set; } = null!;
    public Disease? Disease { get; set; }
}
