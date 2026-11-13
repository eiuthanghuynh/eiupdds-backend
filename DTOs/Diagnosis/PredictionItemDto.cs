namespace eiupdds_backend.DTOs.Diagnosis;

public class PredictionItemDto
{
    public int Id { get; set; }
    public int? DiseaseId { get; set; }
    public string? DiseaseName { get; set; }
    public string DetectedClass { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public double Confidence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string? DetectionId { get; set; }
}
