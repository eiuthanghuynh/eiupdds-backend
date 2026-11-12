using System.Text;
using System.Text.Json;
using eiupdds_backend.DTOs.Roboflow;
using eiupdds_backend.Services.Interfaces;

namespace eiupdds_backend.Services.Implementations;

public class RoboflowService : IRoboflowService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RoboflowService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RoboflowService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RoboflowService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RoboflowModelOutput> DetectDiseaseAsync(string imagePhysicalPath)
    {
        if (!File.Exists(imagePhysicalPath))
        {
            throw new FileNotFoundException($"Image file does not exist at '{imagePhysicalPath}'.", imagePhysicalPath);
        }

        var apiKey = Environment.GetEnvironmentVariable("ROBOFLOW_API_KEY")
            ?? _configuration["Roboflow:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Roboflow API key is not configured. Please set the ROBOFLOW_API_KEY environment variable or define Roboflow:ApiKey in appsettings.");
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePhysicalPath);
        var base64Image = Convert.ToBase64String(imageBytes);

        var payload = new RoboflowWorkflowRequest
        {
            ApiKey = apiKey,
            Inputs = new RoboflowWorkflowInputs
            {
                Image = new RoboflowImageInput
                {
                    Type = "base64",
                    Value = base64Image
                }
            }
        };

        var baseUrl = _configuration["Roboflow:BaseUrl"] ?? "https://serverless.roboflow.com";
        var workspace = _configuration["Roboflow:Workspace"] ?? "plant-disease-detection-mztfb";
        var workflowId = _configuration["Roboflow:WorkflowId"] ?? "plant-disease-yolo-logic";
        var endpoint = $"{baseUrl.TrimEnd('/')}/{workspace}/workflows/{workflowId}";

        var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);
        using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, httpContent);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Roboflow inference request failed with status {StatusCode} ({Reason})", response.StatusCode, response.ReasonPhrase);
            response.EnsureSuccessStatusCode();
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException("Received empty response payload from Roboflow inference server.");
        }

        List<RoboflowWorkflowResponseItem>? resultItems;
        try
        {
            resultItems = JsonSerializer.Deserialize<List<RoboflowWorkflowResponseItem>>(responseBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response from Roboflow: {RawResponse}", responseBody);
            throw new InvalidOperationException("Failed to deserialize Roboflow workflow response.", ex);
        }

        var firstOutput = resultItems?.FirstOrDefault()?.ModelOutput;
        if (firstOutput == null)
        {
            throw new InvalidOperationException("Roboflow response does not contain valid prediction data.");
        }

        return firstOutput;
    }
}
