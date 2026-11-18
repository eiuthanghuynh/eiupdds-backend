using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Diseases;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class DiseaseServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenFilteredByPlantId_ShouldReturnOnlyDiseasesOfThatPlant()
    {
        await using var context = CreateContext();

        var tomato = new Plant { Name = "Tomato", IsActive = true, CreatedAt = DateTime.UtcNow };
        var potato = new Plant { Name = "Potato", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Plants.AddRange(tomato, potato);
        await context.SaveChangesAsync();

        context.Diseases.AddRange(
            new Disease
            {
                PlantId = tomato.Id,
                Name = "Tomato Early Blight",
                RoboflowClassId = 0,
                RoboflowLabel = "tomato-early-blight",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Disease
            {
                PlantId = tomato.Id,
                Name = "Tomato Late Blight",
                RoboflowClassId = 1,
                RoboflowLabel = "tomato-late-blight",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Disease
            {
                PlantId = potato.Id,
                Name = "Potato Early Blight",
                RoboflowClassId = 5,
                RoboflowLabel = "potato-early-blight",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var service = new DiseaseService(context);

        var result = await service.GetAllAsync(tomato.Id, search: null);

        Assert.Equal(2, result.Count);
        Assert.All(result, disease => Assert.Equal(tomato.Id, disease.PlantId));
        Assert.Contains(result, d => d.Name == "Tomato Early Blight");
        Assert.DoesNotContain(result, d => d.Name == "Potato Early Blight");
    }

    [Fact]
    public async Task CreateUpdateGetByIdAndSoftDeleteAsync_ShouldManageDiseaseLifecycle()
    {
        await using var context = CreateContext();
        var tomato = new Plant { Name = "Tomato", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Plants.Add(tomato);
        await context.SaveChangesAsync();

        var service = new DiseaseService(context);

        var created = await service.CreateAsync(new CreateDiseaseDto
        {
            PlantId = tomato.Id,
            Name = "Tomato Septoria Leaf Spot",
            Description = "Fungal leaf spots",
            RoboflowClassId = 20,
            RoboflowLabel = "Tomato Septoria leaf spot"
        });

        Assert.True(created.Id > 0);
        Assert.Equal("Tomato", created.PlantName);
        Assert.True(created.IsActive);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.Equal(created.Name, fetched.Name);

        var updated = await service.UpdateAsync(created.Id, new UpdateDiseaseDto
        {
            PlantId = tomato.Id,
            Name = "Tomato Septoria Spot",
            Description = "Updated description",
            RoboflowClassId = 20,
            RoboflowLabel = "Tomato Septoria leaf spot"
        });

        Assert.Equal("Tomato Septoria Spot", updated.Name);
        Assert.NotNull(updated.UpdatedAt);

        await service.SoftDeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(created.Id));
        var archived = await context.Diseases.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == created.Id);
        Assert.NotNull(archived);
        Assert.False(archived.IsActive);
        Assert.NotNull(archived.DeletedAt);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"disease-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
