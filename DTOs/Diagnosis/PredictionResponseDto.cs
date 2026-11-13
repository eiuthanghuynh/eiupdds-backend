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

/// <summary>
/// Detected bounding box and class prediction item.
/// </summary>
public class PredictionItemDto
{
    public string Class { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
