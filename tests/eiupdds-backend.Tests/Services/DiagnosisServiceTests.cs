using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Diagnosis;
using eiupdds_backend.DTOs.Roboflow;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class DiagnosisServiceTests
{
    private readonly Mock<IFileUploadService> _fileUploadServiceMock;
    private readonly Mock<IRoboflowService> _roboflowServiceMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<DiagnosisService>> _loggerMock;

    public DiagnosisServiceTests()
    {
        _fileUploadServiceMock = new Mock<IFileUploadService>();
        _roboflowServiceMock = new Mock<IRoboflowService>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<DiagnosisService>>();

        _environmentMock.Setup(e => e.WebRootPath).Returns(@"C:\app\wwwroot");
        _environmentMock.Setup(e => e.ContentRootPath).Returns(@"C:\app");
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private IDiagnosisService CreateService(ApplicationDbContext dbContext)
    {
        return new DiagnosisService(
            _fileUploadServiceMock.Object,
            _roboflowServiceMock.Object,
            dbContext,
            _environmentMock.Object,
            _loggerMock.Object);
    }

    private static Mock<IFormFile> CreateMockFormFile(
        string fileName = "leaf.jpg",
        long length = 1024,
        string contentType = "image/jpeg")
    {
        var fileMock = new Mock<IFormFile>();
        var content = "fake image content byte sequence";
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.OpenReadStream()).Returns(memoryStream);

        return fileMock;
    }

    [Fact]
    public async Task PredictAsync_ValidImageAndUser_SavesToDbAndReturnsResponse()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        var fileMock = CreateMockFormFile();
        const string userId = "test-user-id-001";
        const string relativeUrl = "/uploads/test-image.jpg";

        _fileUploadServiceMock
            .Setup(s => s.UploadImageAsync(fileMock.Object))
            .ReturnsAsync(relativeUrl);

        var roboflowOutput = new RoboflowModelOutput
        {
            Image = new RoboflowImageMeta { Width = 1024, Height = 768 },
            Predictions = new List<RoboflowPredictionDetail>
            {
                new()
                {
                    ClassName = "Tomato Early Blight",
                    ClassId = 1,
                    Confidence = 0.88,
                    X = 150.5,
                    Y = 200.0,
                    Width = 80.0,
                    Height = 90.0,
                    DetectionId = "det-001"
                }
            }
        };

        _roboflowServiceMock
            .Setup(s => s.DetectDiseaseAsync(It.IsAny<string>()))
            .ReturnsAsync(roboflowOutput);

        // Act
        var result = await service.PredictAsync(fileMock.Object, userId);

        // Assert - Return DTO
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(relativeUrl, result.ImageUrl);
        Assert.Single(result.Predictions);

        var itemDto = result.Predictions[0];
        Assert.Equal("Tomato Early Blight", itemDto.Class);
        Assert.Equal(0.88, itemDto.Confidence);
        Assert.Equal(150.5, itemDto.X);
        Assert.Equal(200.0, itemDto.Y);
        Assert.Equal(80.0, itemDto.Width);
        Assert.Equal(90.0, itemDto.Height);

        // Assert - Database persistence
        var savedHistory = await dbContext.PredictionHistories
            .Include(h => h.PredictionItems)
            .FirstOrDefaultAsync(h => h.Id == result.Id);

        Assert.NotNull(savedHistory);
        Assert.Equal(userId, savedHistory.UserId);
        Assert.Equal(relativeUrl, savedHistory.ImageUrl);
        Assert.Equal(1024, savedHistory.ImageWidth);
        Assert.Equal(768, savedHistory.ImageHeight);
        Assert.Single(savedHistory.PredictionItems);

        var savedItem = savedHistory.PredictionItems.First();
        Assert.Equal("Tomato Early Blight", savedItem.DetectedClass);
        Assert.Equal(1, savedItem.ClassId);
        Assert.Equal(0.88, savedItem.Confidence);
        Assert.Equal(150.5, savedItem.X);
        Assert.Equal(200.0, savedItem.Y);
        Assert.Equal(80.0, savedItem.Width);
        Assert.Equal(90.0, savedItem.Height);
        Assert.Equal("det-001", savedItem.DetectionId);
    }

    [Fact]
    public async Task PredictAsync_WhenRoboflowThrows_CleansUpUploadedImageAndRethrows()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        var fileMock = CreateMockFormFile();
        const string userId = "test-user-id-002";
        const string uploadedPath = "/uploads/temp-to-fail.jpg";

        _fileUploadServiceMock
            .Setup(s => s.UploadImageAsync(fileMock.Object))
            .ReturnsAsync(uploadedPath);

        _roboflowServiceMock
            .Setup(s => s.DetectDiseaseAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Roboflow connection timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.PredictAsync(fileMock.Object, userId));

        // Verify cleanup was invoked exactly once for the uploaded file
        _fileUploadServiceMock.Verify(s => s.DeleteImage(uploadedPath), Times.Once);

        // Verify no history or items were saved in database
        Assert.Empty(dbContext.PredictionHistories);
        Assert.Empty(dbContext.PredictionItems);
    }

    [Fact]
    public async Task PredictAsync_NullOrEmptyFile_ThrowsArgumentException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        // Act & Assert - Null file
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.PredictAsync(null!, "valid-user-id"));

        // Act & Assert - Empty file (length = 0)
        var emptyFileMock = CreateMockFormFile(length: 0);
        await Assert.ThrowsAsync<ArgumentException>(() => service.PredictAsync(emptyFileMock.Object, "valid-user-id"));

        // Verify dependencies were never called
        _fileUploadServiceMock.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
        _roboflowServiceMock.Verify(s => s.DetectDiseaseAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PredictAsync_NullOrEmptyUserId_ThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        var fileMock = CreateMockFormFile();

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.PredictAsync(fileMock.Object, invalidUserId!));

        // Verify dependencies were never called
        _fileUploadServiceMock.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>()), Times.Never);
        _roboflowServiceMock.Verify(s => s.DetectDiseaseAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PredictAsync_KnownDiseaseInDatabase_LinksDiseaseIdAndPlantId()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();

        var plant = new Plant
        {
            Name = "Potato",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Plants.Add(plant);
        await dbContext.SaveChangesAsync();

        var disease = new Disease
        {
            PlantId = plant.Id,
            Name = "Potato Leaf Late Blight",
            RoboflowLabel = "Potato leaf late blight",
            RoboflowClassId = 13,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Diseases.Add(disease);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var fileMock = CreateMockFormFile();
        const string userId = "test-user-id-003";
        const string relativeUrl = "/uploads/potato-late-blight.jpg";

        _fileUploadServiceMock
            .Setup(s => s.UploadImageAsync(fileMock.Object))
            .ReturnsAsync(relativeUrl);

        var roboflowOutput = new RoboflowModelOutput
        {
            Image = new RoboflowImageMeta { Width = 1600, Height = 1067 },
            Predictions = new List<RoboflowPredictionDetail>
            {
                new()
                {
                    ClassName = "Potato leaf late blight",
                    ClassId = 13,
                    Confidence = 0.94,
                    X = 500.0,
                    Y = 400.0,
                    Width = 200.0,
                    Height = 250.0,
                    DetectionId = "det-potato-01"
                }
            }
        };

        _roboflowServiceMock
            .Setup(s => s.DetectDiseaseAsync(It.IsAny<string>()))
            .ReturnsAsync(roboflowOutput);

        // Act
        var result = await service.PredictAsync(fileMock.Object, userId);

        // Assert
        Assert.NotNull(result);
        var savedHistory = await dbContext.PredictionHistories
            .Include(h => h.PredictionItems)
            .FirstOrDefaultAsync(h => h.Id == result.Id);

        Assert.NotNull(savedHistory);
        Assert.Equal(plant.Id, savedHistory.PlantId);
        Assert.Single(savedHistory.PredictionItems);

        var savedItem = savedHistory.PredictionItems.First();
        Assert.Equal(disease.Id, savedItem.DiseaseId);
        Assert.Equal("Potato leaf late blight", savedItem.DetectedClass);
        Assert.Equal(13, savedItem.ClassId);
    }

    [Fact]
    public async Task PredictAsync_MultiplePredictions_SavesAllPredictionItems()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        var fileMock = CreateMockFormFile();
        const string userId = "test-user-id-004";
        const string relativeUrl = "/uploads/multi-lesion.jpg";

        _fileUploadServiceMock
            .Setup(s => s.UploadImageAsync(fileMock.Object))
            .ReturnsAsync(relativeUrl);

        var roboflowOutput = new RoboflowModelOutput
        {
            Image = new RoboflowImageMeta { Width = 1920, Height = 1080 },
            Predictions = new List<RoboflowPredictionDetail>
            {
                new()
                {
                    ClassName = "Lesion Spot A",
                    ClassId = 1,
                    Confidence = 0.91,
                    X = 100.0,
                    Y = 150.0,
                    Width = 40.0,
                    Height = 45.0,
                    DetectionId = "det-multi-1"
                },
                new()
                {
                    ClassName = "Lesion Spot B",
                    ClassId = 2,
                    Confidence = 0.85,
                    X = 300.0,
                    Y = 350.0,
                    Width = 60.0,
                    Height = 65.0,
                    DetectionId = "det-multi-2"
                }
            }
        };

        _roboflowServiceMock
            .Setup(s => s.DetectDiseaseAsync(It.IsAny<string>()))
            .ReturnsAsync(roboflowOutput);

        // Act
        var result = await service.PredictAsync(fileMock.Object, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Predictions.Count);

        var savedHistory = await dbContext.PredictionHistories
            .Include(h => h.PredictionItems)
            .FirstOrDefaultAsync(h => h.Id == result.Id);

        Assert.NotNull(savedHistory);
        Assert.Equal(2, savedHistory.PredictionItems.Count);
        Assert.Contains(savedHistory.PredictionItems, pi => pi.DetectedClass == "Lesion Spot A" && pi.ClassId == 1);
        Assert.Contains(savedHistory.PredictionItems, pi => pi.DetectedClass == "Lesion Spot B" && pi.ClassId == 2);
    }

    [Fact]
    public async Task PredictAsync_UnknownDiseaseInDatabase_LeavesDiseaseIdAndPlantIdAsNull()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var service = CreateService(dbContext);

        var fileMock = CreateMockFormFile();
        const string userId = "test-user-id-005";
        const string relativeUrl = "/uploads/unknown-disease.jpg";

        _fileUploadServiceMock
            .Setup(s => s.UploadImageAsync(fileMock.Object))
            .ReturnsAsync(relativeUrl);

        var roboflowOutput = new RoboflowModelOutput
        {
            Image = new RoboflowImageMeta { Width = 800, Height = 600 },
            Predictions = new List<RoboflowPredictionDetail>
            {
                new()
                {
                    ClassName = "Unregistered Wild Fungus",
                    ClassId = 999,
                    Confidence = 0.77,
                    X = 250.0,
                    Y = 250.0,
                    Width = 50.0,
                    Height = 50.0,
                    DetectionId = "det-unknown"
                }
            }
        };

        _roboflowServiceMock
            .Setup(s => s.DetectDiseaseAsync(It.IsAny<string>()))
            .ReturnsAsync(roboflowOutput);

        // Act
        var result = await service.PredictAsync(fileMock.Object, userId);

        // Assert
        Assert.NotNull(result);
        var savedHistory = await dbContext.PredictionHistories
            .Include(h => h.PredictionItems)
            .FirstOrDefaultAsync(h => h.Id == result.Id);

        Assert.NotNull(savedHistory);
        Assert.Null(savedHistory.PlantId);
        Assert.Single(savedHistory.PredictionItems);

        var savedItem = savedHistory.PredictionItems.First();
        Assert.Null(savedItem.DiseaseId);
        Assert.Equal("Unregistered Wild Fungus", savedItem.DetectedClass);
    }
}
