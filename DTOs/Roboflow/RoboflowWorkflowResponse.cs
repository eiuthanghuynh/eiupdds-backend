using System.Text.Json.Serialization;

namespace eiupdds_backend.DTOs.Roboflow;

public class RoboflowWorkflowResponseItem
{
    [JsonPropertyName("model_output")]
    public RoboflowModelOutput ModelOutput { get; set; } = new();
}

public class RoboflowModelOutput
{
    [JsonPropertyName("image")]
    public RoboflowImageMeta Image { get; set; } = new();

    [JsonPropertyName("predictions")]
    public List<RoboflowPredictionDetail> Predictions { get; set; } = new();
}

public class RoboflowImageMeta
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class RoboflowPredictionDetail
{
    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }

    [JsonPropertyName("class")]
    public string ClassName { get; set; } = string.Empty;

    [JsonPropertyName("detection_id")]
    public string DetectionId { get; set; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public string ParentId { get; set; } = string.Empty;
}
