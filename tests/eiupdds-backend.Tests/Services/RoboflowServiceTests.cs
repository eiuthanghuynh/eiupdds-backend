using System.Net;
using System.Text;
using eiupdds_backend.DTOs.Roboflow;
using eiupdds_backend.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class RoboflowServiceTests
{
    private const string SampleRoboflowSuccessResponseJson = @"[
        {
            ""model_output"": {
                ""image"": {
                    ""width"": 1600,
                    ""height"": 1067
                },
                ""predictions"": [
                    {
                        ""width"": 353,
                        ""height"": 556,
                        ""x"": 763.5,
                        ""y"": 636,
                        ""confidence"": 0.7315,
                        ""class_id"": 13,
                        ""class"": ""Potato leaf late blight"",
                        ""detection_id"": ""d77419eb-14fb-4c4a-8dff-35a041968fb4"",
                        ""parent_id"": ""image""
                    }
                ]
            }
        }
    ]";

    [Fact]
    public async Task DetectDiseaseAsync_ValidImage_ReturnsParsedModelOutput()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SampleRoboflowSuccessResponseJson, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Roboflow:ApiKey", "mock_roboflow_api_key_valid" }
                })
                .Build();
            var mockLogger = new Mock<ILogger<RoboflowService>>();

            var service = new RoboflowService(httpClient, configuration, mockLogger.Object);

            var result = await service.DetectDiseaseAsync(tempFile);

            Assert.NotNull(result);
            Assert.NotNull(result.Image);
            Assert.Equal(1600, result.Image.Width);
            Assert.Equal(1067, result.Image.Height);

            Assert.NotNull(result.Predictions);
            Assert.Single(result.Predictions);

            var prediction = result.Predictions[0];
            Assert.Equal("Potato leaf late blight", prediction.ClassName);
            Assert.Equal(0.7315, prediction.Confidence);
            Assert.Equal(13, prediction.ClassId);
            Assert.Equal("d77419eb-14fb-4c4a-8dff-35a041968fb4", prediction.DetectionId);
            Assert.Equal("image", prediction.ParentId);
            Assert.Equal(353, prediction.Width);
            Assert.Equal(556, prediction.Height);
            Assert.Equal(763.5, prediction.X);
            Assert.Equal(636, prediction.Y);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DetectDiseaseAsync_NonExistentImage_ThrowsFileNotFoundException()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Roboflow:ApiKey", "mock_roboflow_api_key" }
            })
            .Build();
        var mockLogger = new Mock<ILogger<RoboflowService>>();

        var service = new RoboflowService(httpClient, configuration, mockLogger.Object);
        var nonExistentPath = @"C:\non_existent_image_12345.jpg";

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.DetectDiseaseAsync(nonExistentPath));
    }

    [Fact]
    public async Task DetectDiseaseAsync_NoApiKeyConfigured_ThrowsInvalidOperationException()
    {
        var originalEnv = Environment.GetEnvironmentVariable("ROBOFLOW_API_KEY");
        var tempFile = Path.GetTempFileName();
        try
        {
            Environment.SetEnvironmentVariable("ROBOFLOW_API_KEY", null);

            var mockHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHandler.Object);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();
            var mockLogger = new Mock<ILogger<RoboflowService>>();

            var service = new RoboflowService(httpClient, configuration, mockLogger.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DetectDiseaseAsync(tempFile));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ROBOFLOW_API_KEY", originalEnv);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DetectDiseaseAsync_RoboflowReturns401_ThrowsHttpRequestException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\": \"Unauthorized - Invalid API Key\"}", Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Roboflow:ApiKey", "invalid_api_key" }
                })
                .Build();
            var mockLogger = new Mock<ILogger<RoboflowService>>();

            var service = new RoboflowService(httpClient, configuration, mockLogger.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.DetectDiseaseAsync(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
