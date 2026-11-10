using System.Text.Json.Serialization;

namespace eiupdds_backend.DTOs.Roboflow;

/// <summary>
/// Request payload sent to the Roboflow Serverless Workflow endpoint.
/// </summary>
public class RoboflowWorkflowRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("inputs")]
    public RoboflowWorkflowInputs Inputs { get; set; } = new();
}

/// <summary>
/// Container holding input parameters defined in the Roboflow workflow.
/// </summary>
public class RoboflowWorkflowInputs
{
    [JsonPropertyName("image")]
    public RoboflowImageInput Image { get; set; } = new();
}

/// <summary>
/// Base64-encoded image input for inference.
/// </summary>
public class RoboflowImageInput
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
