using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Plants;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class PlantServiceTests
{
    [Fact]
    public async Task GetAllAsync_WithPaginationSearchAndInvalidBounds_ReturnsClampedPagedPlants()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = DateTime.UtcNow;

        context.Plants.AddRange(
            new Plant { Name = "Tomato", IsActive = true, CreatedAt = now },
            new Plant { Name = "Potato", IsActive = true, CreatedAt = now.AddMinutes(1) },
            new Plant { Name = "Bell Pepper", IsActive = true, CreatedAt = now.AddMinutes(2) },
            new Plant { Name = "Deleted Plant", IsActive = false, CreatedAt = now.AddMinutes(3), DeletedAt = now });
        await context.SaveChangesAsync();

        var service = new PlantService(context, cache);

        var clampedResult = await service.GetAllAsync(0, 0, search: null, sortBy: "name", isDescending: false);

        Assert.Equal(1, clampedResult.PageNumber);
        Assert.Equal(10, clampedResult.PageSize);
        Assert.Equal(3, clampedResult.TotalCount);
        Assert.Equal(1, clampedResult.TotalPages);
        Assert.Equal(["Bell Pepper", "Potato", "Tomato"], clampedResult.Items.Select(p => p.Name));

        var filteredResult = await service.GetAllAsync(1, 1, search: "tom", sortBy: "name", isDescending: false);

        Assert.Equal(1, filteredResult.TotalCount);
        Assert.Single(filteredResult.Items);
        Assert.Equal("Tomato", filteredResult.Items[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenCachePopulated_ServesCachedListAndInvalidatesOnWrite()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        context.Plants.Add(new Plant { Name = "Apple", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new PlantService(context, cache);

        var firstRead = await service.GetAllAsync(1, 10, null, null, false);
        Assert.Equal(1, firstRead.TotalCount);

        context.Plants.Add(new Plant { Name = "Banana", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var cachedRead = await service.GetAllAsync(1, 10, null, null, false);
        Assert.Equal(1, cachedRead.TotalCount);

        await service.CreateAsync(new CreatePlantDto { Name = "Cherry" });

        var refreshedRead = await service.GetAllAsync(1, 10, null, null, false);
        Assert.Equal(3, refreshedRead.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPlantExists_ReturnsOnlyActiveDiseases()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = DateTime.UtcNow;

        var tomato = new Plant { Name = "Tomato", IsActive = true, CreatedAt = now };
        context.Plants.Add(tomato);
        await context.SaveChangesAsync();

        context.Diseases.AddRange(
            new Disease
            {
                PlantId = tomato.Id,
                Name = "Tomato Early Blight",
                RoboflowClassId = 19,
                RoboflowLabel = "Tomato Early blight leaf",
                IsActive = true,
                CreatedAt = now
            },
            new Disease
            {
                PlantId = tomato.Id,
                Name = "Archived Disease",
                RoboflowClassId = 99,
                RoboflowLabel = "archived-disease",
                IsActive = false,
                CreatedAt = now,
                DeletedAt = now
            });
        await context.SaveChangesAsync();

        var service = new PlantService(context, cache);

        var result = await service.GetByIdAsync(tomato.Id);

        Assert.Equal("Tomato", result.Name);
        Assert.Single(result.Diseases);
        Assert.Equal("Tomato Early Blight", result.Diseases[0].Name);
    }

    [Fact]
    public async Task CreateAsync_WhenPlantNameAlreadyExists_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        context.Plants.Add(new Plant { Name = "Tomato", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new PlantService(context, cache);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreatePlantDto { Name = "  tomato  " }));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenPlantHasActiveDiseases_DeactivatesPlantAndCascadesDiseases()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = DateTime.UtcNow;

        var tomato = new Plant { Name = "Tomato", IsActive = true, CreatedAt = now };
        context.Plants.Add(tomato);
        await context.SaveChangesAsync();

        var disease = new Disease
        {
            PlantId = tomato.Id,
            Name = "Tomato Late Blight",
            RoboflowClassId = 23,
            RoboflowLabel = "Tomato leaf late blight",
            IsActive = true,
            CreatedAt = now
        };
        context.Diseases.Add(disease);
        await context.SaveChangesAsync();

        var service = new PlantService(context, cache);

        await service.SoftDeleteAsync(tomato.Id);

        var deletedPlant = await context.Plants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == tomato.Id);
        var deletedDisease = await context.Diseases
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == disease.Id);

        Assert.NotNull(deletedPlant);
        Assert.False(deletedPlant.IsActive);
        Assert.NotNull(deletedPlant.DeletedAt);

        Assert.NotNull(deletedDisease);
        Assert.False(deletedDisease.IsActive);
        Assert.NotNull(deletedDisease.DeletedAt);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(tomato.Id));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"plant-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
