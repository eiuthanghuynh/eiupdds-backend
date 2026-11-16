using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Numerics;
using Xunit;
namespace eiupdds_backend.Tests.Services;

public class PlantServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
        return new ApplicationDbContext(options);
    }
    [Fact]
    public async Task GetAllPlantsAsync_WithPagination_ReturnsCorrectItems()
    {
        using var context = CreateInMemoryDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PlantService(context, cache);
        // Chuẩn bị 3 cây
        context.Plants.AddRange(
        new Plant { Name = "Cà chua", IsActive = true, CreatedAt = DateTime.UtcNow },
        new Plant { Name = "Khoai tây", IsActive = true, CreatedAt = DateTime.UtcNow },
        new Plant { Name = "Bắp cải", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();
        var query = new PlantQueryParameters { PageNumber = 1, PageSize = 2 };
        var result = await service.GetAllPlantsAsync(query);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
    }
    [Fact]
    public async Task SoftDeletePlantAsync_ExistingPlant_SetsIsActiveToFalse()
    {
        using var context = CreateInMemoryDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PlantService(context, cache);
        var plant = new Plant { Name = "Ớt chuông", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Plants.Add(plant);
        await context.SaveChangesAsync();
        var success = await service.SoftDeletePlantAsync(plant.Id);
        Assert.True(success);
        var deletedPlant = await context.Plants.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id ==
        plant.Id);
        Assert.NotNull(deletedPlant);
        Assert.False(deletedPlant.IsActive);
        Assert.NotNull(deletedPlant.DeletedAt);
    }
}
