using System.Text.Json.Serialization;

namespace eiupdds_backend.DTOs.Roboflow;

public class RoboflowWorkflowRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("inputs")]
    public RoboflowWorkflowInputs Inputs { get; set; } = new();
}

public class RoboflowWorkflowInputs
{
    [JsonPropertyName("image")]
    public RoboflowImageInput Image { get; set; } = new();
}

public class RoboflowImageInput
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
