namespace eiupdds_backend.DTOs.Diagnosis;

public class PredictionItemDto
{
    private string? _class;

    public int Id { get; set; }
    public int ClassId { get; set; }
    public string DetectedClass { get; set; } = string.Empty;

    public string Class
    {
        get => string.IsNullOrEmpty(_class) ? DetectedClass : _class;
        set
        {
            _class = value;
            if (string.IsNullOrEmpty(DetectedClass))
            {
                DetectedClass = value;
            }
        }
    }

    public double Confidence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string? DetectionId { get; set; }
}