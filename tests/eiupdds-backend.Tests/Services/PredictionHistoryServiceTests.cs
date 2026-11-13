using eiupdds_backend.Data;
using eiupdds_backend.Models;
using eiupdds_backend.Repositories.Implementations;
using eiupdds_backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
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

        var service = new PredictionHistoryService(new PredictionHistoryRepository(context));

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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"history-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
