using System.Text;
using eiupdds_backend.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class FileUploadServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FileUploadService _service;

    public FileUploadServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"eiupdds-upload-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_tempRoot);
        envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);

        _service = new FileUploadService(envMock.Object, new Mock<ILogger<FileUploadService>>().Object);
    }

    [Fact]
    public async Task UploadImageAsync_WhenValidJpg_SavesFileAndReturnsRelativeUrl()
    {
        var formFile = CreateFormFile("leaf.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var relativeUrl = await _service.UploadImageAsync(formFile);

        Assert.StartsWith("/uploads/", relativeUrl);
        Assert.EndsWith(".jpg", relativeUrl);

        var savedPath = Path.Combine(_tempRoot, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(savedPath));
    }

    [Theory]
    [InlineData("script.exe", "image/jpeg")]
    [InlineData("leaf.jpg", "application/octet-stream")]
    public async Task UploadImageAsync_WhenInvalidExtensionOrMimeType_ThrowsArgumentException(string fileName, string contentType)
    {
        var formFile = CreateFormFile(fileName, contentType, Encoding.UTF8.GetBytes("test"));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadImageAsync(formFile));
    }

    [Fact]
    public async Task UploadImageAsync_WhenFileExceedsLimit_ThrowsArgumentException()
    {
        var oversizedBytes = new byte[(5 * 1024 * 1024) + 1];
        var formFile = CreateFormFile("big.jpg", "image/jpeg", oversizedBytes);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadImageAsync(formFile));
    }

    [Fact]
    public async Task DeleteImage_WhenFileExists_RemovesFileFromDisk()
    {
        var formFile = CreateFormFile("to-delete.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var relativeUrl = await _service.UploadImageAsync(formFile);

        var deleted = _service.DeleteImage(relativeUrl);

        Assert.True(deleted);
        var savedPath = Path.Combine(_tempRoot, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.False(File.Exists(savedPath));
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
