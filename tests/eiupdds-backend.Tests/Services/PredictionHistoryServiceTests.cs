using eiupdds_backend.Data;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class PredictionHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_WhenUserHasRecords_ShouldReturnOnlyThatUsersHistory()
    {
        await using var context = CreateContext();

        var currentUserId = "user-a";
        var otherUserId = "user-b";

        context.PredictionHistories.AddRange(
            new PredictionHistory
            {
                UserId = currentUserId,
                ImageUrl = "/uploads/leaf-a.jpg",
                ImageWidth = 640,
                ImageHeight = 480,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                PredictionItems =
                {
                    new PredictionItem
                    {
                        DetectedClass = "tomato-early-blight",
                        ClassId = 0,
                        Confidence = 0.91,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                }
            },
            new PredictionHistory
            {
                UserId = currentUserId,
                ImageUrl = "/uploads/leaf-b.jpg",
                ImageWidth = 800,
                ImageHeight = 600,
                CreatedAt = DateTime.UtcNow,
                PredictionItems =
                {
                    new PredictionItem
                    {
                        DetectedClass = "tomato-late-blight",
                        ClassId = 1,
                        Confidence = 0.77,
                        CreatedAt = DateTime.UtcNow
                    },
                    new PredictionItem
                    {
                        DetectedClass = "tomato-leaf-mold",
                        ClassId = 2,
                        Confidence = 0.64,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            },
            new PredictionHistory
            {
                UserId = otherUserId,
                ImageUrl = "/uploads/other.jpg",
                ImageWidth = 320,
                ImageHeight = 240,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var service = new PredictionHistoryService(context, new Mock<IFileUploadService>().Object);

        var result = await service.GetHistoryAsync(
            currentUserId,
            pageNumber: 1,
            pageSize: 10,
            fromDate: null,
            toDate: null);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.DoesNotContain("other.jpg", item.ImageUrl));
        Assert.Equal("/uploads/leaf-b.jpg", result.Items[0].ImageUrl);
        Assert.Equal(2, result.Items[0].DetectionCount);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenToDateIsDateOnly_IncludesEntireDay()
    {
        await using var context = CreateContext();
        var userId = "user-date";
        var targetDate = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);

        context.PredictionHistories.AddRange(
            new PredictionHistory
            {
                UserId = userId,
                ImageUrl = "/uploads/in-day.jpg",
                CreatedAt = new DateTime(2026, 11, 18, 14, 30, 0, DateTimeKind.Utc)
            },
            new PredictionHistory
            {
                UserId = userId,
                ImageUrl = "/uploads/next-day.jpg",
                CreatedAt = new DateTime(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var service = new PredictionHistoryService(context, new Mock<IFileUploadService>().Object);

        var result = await service.GetHistoryAsync(userId, 1, 10, fromDate: targetDate, toDate: targetDate);

        Assert.Single(result.Items);
        Assert.Equal("/uploads/in-day.jpg", result.Items[0].ImageUrl);
    }

    [Fact]
    public async Task GetByIdAndDeleteAsync_ShouldIsolateByUserAndDeleteUploadedImage()
    {
        await using var context = CreateContext();
        var history = new PredictionHistory
        {
            UserId = "owner-user",
            ImageUrl = "/uploads/owner-leaf.jpg",
            ImageWidth = 500,
            ImageHeight = 500,
            CreatedAt = DateTime.UtcNow
        };
        context.PredictionHistories.Add(history);
        await context.SaveChangesAsync();

        var fileUploadMock = new Mock<IFileUploadService>();
        var service = new PredictionHistoryService(context, fileUploadMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(history.Id, "other-user"));

        var detail = await service.GetByIdAsync(history.Id, "owner-user");
        Assert.Equal(history.Id, detail.Id);

        await service.DeleteAsync(history.Id, "owner-user");

        Assert.Empty(context.PredictionHistories);
        fileUploadMock.Verify(s => s.DeleteImage("/uploads/owner-leaf.jpg"), Times.Once);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"history-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
