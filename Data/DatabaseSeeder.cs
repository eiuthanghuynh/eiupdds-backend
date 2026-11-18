using eiupdds_backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Data;

public static class DatabaseSeeder
{
    public static void SeedAdmin(ApplicationDbContext dbContext, IPasswordHasher<ApplicationUser> passwordHasher)
    {
        const string adminEmail = "admin@eiu.edu.vn";
        if (dbContext.Users.IgnoreQueryFilters().Any(u => u.Email == adminEmail))
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = "Admin",
            Email = adminEmail,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, "123456");

        dbContext.Users.Add(admin);
        dbContext.SaveChanges();
    }

    public static void SeedCatalog(ApplicationDbContext dbContext)
    {
        var appleId = EnsurePlant(dbContext, "Apple");
        var pepperId = EnsurePlant(dbContext, "Pepper");
        var blueberryId = EnsurePlant(dbContext, "Blueberry");
        var cherryId = EnsurePlant(dbContext, "Cherry");
        var cornId = EnsurePlant(dbContext, "Corn");
        var peachId = EnsurePlant(dbContext, "Peach");
        var potatoId = EnsurePlant(dbContext, "Potato");
        var raspberryId = EnsurePlant(dbContext, "Raspberry");
        var soybeanId = EnsurePlant(dbContext, "Soybean");
        var squashId = EnsurePlant(dbContext, "Squash");
        var strawberryId = EnsurePlant(dbContext, "Strawberry");
        var tomatoId = EnsurePlant(dbContext, "Tomato");
        var grapeId = EnsurePlant(dbContext, "Grape");

        var now = DateTime.UtcNow;

        // Apple (Class 0 - 2)
        UpsertDisease(dbContext, appleId, "Apple Scab", "Apple Scab Leaf", 0, "Fungal infection causing olive-green to black velvety spots on apple leaves.", now);
        UpsertDisease(dbContext, appleId, "Apple Healthy", "Apple leaf", 1, "Healthy apple leaf with no visible symptoms.", now);
        UpsertDisease(dbContext, appleId, "Apple Rust", "Apple rust leaf", 2, "Cedar apple rust producing bright orange-yellow spots on upper leaf surfaces.", now);

        // Pepper (Class 3 - 4)
        UpsertDisease(dbContext, pepperId, "Pepper Healthy", "Bell_pepper leaf", 3, "Healthy bell pepper leaf with no visible disease symptoms.", now);
        UpsertDisease(dbContext, pepperId, "Pepper Bacterial Spot", "Bell_pepper leaf spot", 4, "Small water-soaked or necrotic circular lesions on bell pepper leaves.", now);

        // Blueberry (Class 5)
        UpsertDisease(dbContext, blueberryId, "Blueberry Healthy", "Blueberry leaf", 5, "Healthy blueberry leaf with no visible disease symptoms.", now);

        // Cherry (Class 6)
        UpsertDisease(dbContext, cherryId, "Cherry Healthy", "Cherry leaf", 6, "Healthy cherry leaf with no visible disease symptoms.", now);

        // Corn (Class 7 - 9)
        UpsertDisease(dbContext, cornId, "Corn Gray Leaf Spot", "Corn Gray leaf spot", 7, "Rectangular gray-tan lesions running parallel to leaf veins caused by Cercospora zeae-maydis.", now);
        UpsertDisease(dbContext, cornId, "Corn Leaf Blight", "Corn leaf blight", 8, "Long elliptical grayish-green or tan lesions caused by Exserohilum turcicum.", now);
        UpsertDisease(dbContext, cornId, "Corn Rust", "Corn rust leaf", 9, "Small powdery cinnamon-brown pustules scattered across both leaf surfaces.", now);

        // Peach (Class 10)
        UpsertDisease(dbContext, peachId, "Peach Healthy", "Peach leaf", 10, "Healthy peach leaf with no visible disease symptoms.", now);

        // Potato (Class 11 - 13)
        UpsertDisease(dbContext, potatoId, "Potato Healthy", "Potato leaf", 11, "Healthy potato leaf with no visible disease symptoms.", now);
        UpsertDisease(dbContext, potatoId, "Potato Early Blight", "Potato leaf early blight", 12, "Dark brown concentric target-like lesions on potato leaves caused by Alternaria solani.", now);
        UpsertDisease(dbContext, potatoId, "Potato Late Blight", "Potato leaf late blight", 13, "Water-soaked irregular dark lesions that rapidly expand, caused by Phytophthora infestans.", now);

        // Raspberry (Class 14)
        UpsertDisease(dbContext, raspberryId, "Raspberry Healthy", "Raspberry leaf", 14, "Healthy raspberry leaf with no visible disease symptoms.", now);

        // Soybean (Class 15 - 16)
        UpsertDisease(dbContext, soybeanId, "Soybean Healthy (Variant 1)", "Soyabean leaf", 15, "Healthy soybean leaf with normal foliage (dataset spelling variant).", now);
        UpsertDisease(dbContext, soybeanId, "Soybean Healthy (Variant 2)", "Soybean leaf", 16, "Healthy soybean leaf with no visible disease symptoms.", now);

        // Squash (Class 17)
        UpsertDisease(dbContext, squashId, "Squash Powdery Mildew", "Squash Powdery mildew leaf", 17, "White powdery talcum-like fungal spots expanding over squash leaves.", now);

        // Strawberry (Class 18)
        UpsertDisease(dbContext, strawberryId, "Strawberry Healthy", "Strawberry leaf", 18, "Healthy strawberry leaf with no visible disease symptoms.", now);

        // Tomato (Class 19 - 27)
        UpsertDisease(dbContext, tomatoId, "Tomato Early Blight", "Tomato Early blight leaf", 19, "Brown to black concentric rings (target pattern) on older tomato foliage.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Septoria Leaf Spot", "Tomato Septoria leaf spot", 20, "Numerous small circular gray spots with dark borders on tomato leaves.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Healthy", "Tomato leaf", 21, "Healthy tomato leaf with no visible symptoms.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Bacterial Spot", "Tomato leaf bacterial spot", 22, "Small greasy water-soaked circular lesions turning brown with yellow halos.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Late Blight", "Tomato leaf late blight", 23, "Large water-soaked dark patches that rapidly blight leaves in humid conditions.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Mosaic Virus", "Tomato leaf mosaic virus", 24, "Mottled light and dark green patterns with leaf distortion and stunted growth.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Yellow Leaf Curl Virus", "Tomato leaf yellow virus", 25, "Upward curling and yellowing of leaf margins caused by TYLCV transmitted by whiteflies.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Leaf Mold", "Tomato mold leaf", 26, "Pale yellow spots on the upper leaf surface with velvety olive-green mold on the underside.", now);
        UpsertDisease(dbContext, tomatoId, "Tomato Two-Spotted Spider Mites", "Tomato two spotted spider mites leaf", 27, "Fine stippling yellow speckling and webbing caused by Tetranychus urticae.", now);

        // Grape (Class 28 - 29)
        UpsertDisease(dbContext, grapeId, "Grape Healthy", "grape leaf", 28, "Healthy grape leaf with no visible disease symptoms.", now);
        UpsertDisease(dbContext, grapeId, "Grape Black Rot", "grape leaf black rot", 29, "Reddish-brown circular spots with small black fungal fruiting bodies (pycnidia).", now);

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

    private static void UpsertDisease(
        ApplicationDbContext dbContext,
        int plantId,
        string name,
        string roboflowLabel,
        int roboflowClassId,
        string description,
        DateTime timestamp)
    {
        var existing = dbContext.Diseases.FirstOrDefault(d => d.RoboflowClassId == roboflowClassId);
        if (existing is not null)
        {
            existing.PlantId = plantId;
            existing.Name = name;
            existing.RoboflowLabel = roboflowLabel;
            existing.Description = description;
            existing.IsActive = true;
            existing.UpdatedAt = timestamp;
        }
        else
        {
            dbContext.Diseases.Add(new Disease
            {
                PlantId = plantId,
                Name = name,
                RoboflowLabel = roboflowLabel,
                RoboflowClassId = roboflowClassId,
                Description = description,
                IsActive = true,
                CreatedAt = timestamp
            });
        }
    }
}
