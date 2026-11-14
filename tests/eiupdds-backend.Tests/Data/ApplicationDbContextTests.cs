using eiupdds_backend.Data;
using eiupdds_backend.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eiupdds_backend.Tests.Data;

public class ApplicationDbContextTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=dummy;Database=EiupddsDb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Model_ShouldRegisterAllCoreEntities()
    {
        using var context = CreateContext();
        var model = context.Model;

        var userEntity = model.FindEntityType(typeof(ApplicationUser));
        Assert.NotNull(userEntity);
        Assert.Equal("Users", userEntity.GetTableName());

        Assert.NotNull(model.FindEntityType(typeof(Plant)));
        Assert.NotNull(model.FindEntityType(typeof(Disease)));
        Assert.NotNull(model.FindEntityType(typeof(PredictionHistory)));
        Assert.NotNull(model.FindEntityType(typeof(PredictionItem)));
    }

    [Fact]
    public void Model_ShouldConfigureRelationshipsAndCascadeRules()
    {
        using var context = CreateContext();
        var model = context.Model;

        // Disease -> Plant (Restrict)
        var diseaseEntity = model.FindEntityType(typeof(Disease));
        Assert.NotNull(diseaseEntity);
        var plantFk = diseaseEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Plant));
        Assert.NotNull(plantFk);
        Assert.Equal(DeleteBehavior.Restrict, plantFk.DeleteBehavior);

        // PredictionHistory -> User (SetNull)
        var historyEntity = model.FindEntityType(typeof(PredictionHistory));
        Assert.NotNull(historyEntity);
        var userFk = historyEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));
        Assert.NotNull(userFk);
        Assert.Equal(DeleteBehavior.SetNull, userFk.DeleteBehavior);

        // PredictionItem -> PredictionHistory (Cascade)
        var predictionItemEntity = model.FindEntityType(typeof(PredictionItem));
        Assert.NotNull(predictionItemEntity);
        var historyFk = predictionItemEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(PredictionHistory));
        Assert.NotNull(historyFk);
        Assert.Equal(DeleteBehavior.Cascade, historyFk.DeleteBehavior);

        // PredictionItem -> Disease (SetNull)
        var diseaseFk = predictionItemEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Disease));
        Assert.NotNull(diseaseFk);
        Assert.Equal(DeleteBehavior.SetNull, diseaseFk.DeleteBehavior);
    }

    [Fact]
    public void Model_ShouldEnforcePropertyConstraints()
    {
        using var context = CreateContext();
        var model = context.Model;

        var userEntity = model.FindEntityType(typeof(ApplicationUser));
        Assert.NotNull(userEntity);
        var userNameProp = userEntity.FindProperty(nameof(ApplicationUser.FullName));
        Assert.NotNull(userNameProp);
        Assert.False(userNameProp.IsNullable);
        Assert.Equal(100, userNameProp.GetMaxLength());

        var plantEntity = model.FindEntityType(typeof(Plant));
        Assert.NotNull(plantEntity);
        var plantNameProp = plantEntity.FindProperty(nameof(Plant.Name));
        Assert.NotNull(plantNameProp);
        Assert.False(plantNameProp.IsNullable);
        Assert.Equal(150, plantNameProp.GetMaxLength());

        var diseaseEntity = model.FindEntityType(typeof(Disease));
        Assert.NotNull(diseaseEntity);
        var diseaseNameProp = diseaseEntity.FindProperty(nameof(Disease.Name));
        Assert.NotNull(diseaseNameProp);
        Assert.False(diseaseNameProp.IsNullable);
        Assert.Equal(200, diseaseNameProp.GetMaxLength());

        var roboflowLabelProp = diseaseEntity.FindProperty(nameof(Disease.RoboflowLabel));
        Assert.NotNull(roboflowLabelProp);
        Assert.False(roboflowLabelProp.IsNullable);
        Assert.Equal(200, roboflowLabelProp.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldHaveSoftDeleteQueryFilters()
    {
        using var context = CreateContext();
        var model = context.Model;

        var userEntity = model.FindEntityType(typeof(ApplicationUser));
        Assert.NotEmpty(userEntity?.GetDeclaredQueryFilters() ?? []);

        var plantEntity = model.FindEntityType(typeof(Plant));
        Assert.NotEmpty(plantEntity?.GetDeclaredQueryFilters() ?? []);

        var diseaseEntity = model.FindEntityType(typeof(Disease));
        Assert.NotEmpty(diseaseEntity?.GetDeclaredQueryFilters() ?? []);
    }
}
