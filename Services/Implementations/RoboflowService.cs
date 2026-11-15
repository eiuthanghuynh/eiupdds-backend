using System.Net;
using System.Net.Http.Headers;
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

        const int maxRetries = 2;
        var retryDelay = TimeSpan.FromMilliseconds(500);
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode || ((int)response.StatusCode < 500 && response.StatusCode != (HttpStatusCode)429))
                {
                    break;
                }

                _logger.LogWarning("Roboflow inference attempt {Attempt} returned status {StatusCode}. Retrying after {Delay}ms...",
                    attempt + 1, response.StatusCode, retryDelay.TotalMilliseconds);
            }
            catch (Exception ex) when (attempt < maxRetries && (ex is HttpRequestException || ex is TaskCanceledException))
            {
                _logger.LogWarning(ex, "Roboflow inference attempt {Attempt} failed due to network/timeout. Retrying after {Delay}ms...",
                    attempt + 1, retryDelay.TotalMilliseconds);
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(retryDelay);
                retryDelay *= 2;
            }
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Roboflow inference request failed with status {StatusCode} ({Reason})", response?.StatusCode, response?.ReasonPhrase);
            response?.EnsureSuccessStatusCode();
            throw new HttpRequestException("Roboflow inference request failed with no response.");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException("Received empty response payload from Roboflow inference server.");
        }

        RoboflowModelOutput? firstOutput = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<RoboflowWorkflowEnvelope>(responseBody, JsonOptions);
            firstOutput = envelope?.Outputs?.FirstOrDefault()?.ModelOutput;
        }
        catch (JsonException)
        {
        }

        if (firstOutput == null)
        {
            try
            {
                var resultItems = JsonSerializer.Deserialize<List<RoboflowWorkflowResponseItem>>(responseBody, JsonOptions);
                firstOutput = resultItems?.FirstOrDefault()?.ModelOutput;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response from Roboflow: {RawResponse}", responseBody);
                throw new InvalidOperationException("Failed to deserialize Roboflow workflow response.", ex);
            }
        }

        if (firstOutput == null)
        {
            throw new InvalidOperationException("Roboflow response does not contain valid prediction data.");
        }

        return firstOutput;
    }
}
