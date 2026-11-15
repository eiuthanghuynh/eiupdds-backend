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

/// <summary>
/// Detected bounding box and class prediction item.
/// </summary>
public class PredictionItemDto
{
    public int ClassId { get; set; }
    public string Class { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
