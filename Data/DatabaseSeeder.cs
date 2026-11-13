using eiupdds_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Data;

public static class DatabaseSeeder
{
    public static void SeedCatalog(ApplicationDbContext dbContext)
    {
        var tomatoId = EnsurePlant(dbContext, "Tomato");
        var potatoId = EnsurePlant(dbContext, "Potato");
        var pepperId = EnsurePlant(dbContext, "Pepper");
        var cornId = EnsurePlant(dbContext, "Corn");

        if (dbContext.Diseases.Any())
        {
            return;
        }
        var now = DateTime.UtcNow;

        dbContext.Diseases.AddRange(
            CreateDisease(tomatoId, "Tomato Early Blight", "tomato-early-blight", 0, "Dark concentric spots on older tomato leaves.", now),
            CreateDisease(tomatoId, "Tomato Late Blight", "tomato-late-blight", 1, "Water-soaked lesions that rapidly spread on tomato foliage.", now),
            CreateDisease(tomatoId, "Tomato Leaf Mold", "tomato-leaf-mold", 2, "Yellow spots on the upper leaf surface with olive mold underneath.", now),
            CreateDisease(tomatoId, "Tomato Septoria Leaf Spot", "tomato-septoria-leaf-spot", 3, "Small circular spots with dark borders on tomato leaves.", now),
            CreateDisease(tomatoId, "Tomato Healthy", "tomato-healthy", 4, "Healthy tomato leaf with no visible disease symptoms.", now),
            CreateDisease(potatoId, "Potato Early Blight", "potato-early-blight", 5, "Brown target-like lesions on potato leaves.", now),
            CreateDisease(potatoId, "Potato Late Blight", "potato-late-blight", 6, "Irregular water-soaked lesions typical of Phytophthora infestans.", now),
            CreateDisease(potatoId, "Potato Healthy", "potato-healthy", 7, "Healthy potato leaf with no visible disease symptoms.", now),
            CreateDisease(pepperId, "Pepper Bacterial Spot", "pepper-bacterial-spot", 8, "Small water-soaked spots that turn brown on pepper leaves.", now),
            CreateDisease(pepperId, "Pepper Healthy", "pepper-healthy", 9, "Healthy pepper leaf with no visible disease symptoms.", now),
            CreateDisease(cornId, "Corn Common Rust", "corn-common-rust", 10, "Small cinnamon-brown pustules scattered on corn leaves.", now),
            CreateDisease(cornId, "Corn Northern Leaf Blight", "corn-northern-leaf-blight", 11, "Long elliptical gray-green lesions on corn leaves.", now),
            CreateDisease(cornId, "Corn Gray Leaf Spot", "corn-gray-leaf-spot", 12, "Rectangular gray-tan lesions limited by leaf veins.", now),
            CreateDisease(cornId, "Corn Healthy", "corn-healthy", 13, "Healthy corn leaf with no visible disease symptoms.", now));

        dbContext.SaveChanges();
    }

    private static int EnsurePlant(ApplicationDbContext dbContext, string name)
    {
        var plant = dbContext.Plants.FirstOrDefault(p => p.Name == name);
        if (plant is not null)
        {
            return plant.Id;
        }

        plant = new Plant
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Plants.Add(plant);
        dbContext.SaveChanges();
        return plant.Id;
    }

    private static Disease CreateDisease(
        int plantId,
        string name,
        string roboflowLabel,
        int roboflowClassId,
        string description,
        DateTime createdAt)
    {
        return new Disease
        {
            PlantId = plantId,
            Name = name,
            Description = description,
            RoboflowClassId = roboflowClassId,
            RoboflowLabel = roboflowLabel,
            IsActive = true,
            CreatedAt = createdAt
        };
    }
}
