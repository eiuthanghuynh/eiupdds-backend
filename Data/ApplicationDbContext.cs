using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using eiupdds_backend.Models;

namespace eiupdds_backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Disease> Diseases => Set<Disease>();
    public DbSet<PredictionHistory> PredictionHistories => Set<PredictionHistory>();
    public DbSet<PredictionItem> PredictionItems => Set<PredictionItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().HasQueryFilter(u => u.IsActive);
        builder.Entity<Plant>().HasQueryFilter(p => p.IsActive);
        builder.Entity<Disease>().HasQueryFilter(d => d.IsActive);

        builder.Entity<Disease>()
            .HasOne(d => d.Plant)
            .WithMany(p => p.Diseases)
            .HasForeignKey(d => d.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PredictionHistory>()
            .HasOne(ph => ph.User)
            .WithMany(u => u.PredictionHistories)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PredictionHistory>()
            .HasOne(ph => ph.Plant)
            .WithMany(p => p.PredictionHistories)
            .HasForeignKey(ph => ph.PlantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PredictionItem>()
            .HasOne(pi => pi.PredictionHistory)
            .WithMany(ph => ph.PredictionItems)
            .HasForeignKey(pi => pi.PredictionHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PredictionItem>()
            .HasOne(pi => pi.Disease)
            .WithMany(d => d.PredictionItems)
            .HasForeignKey(pi => pi.DiseaseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Plant>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Entity<Disease>()
            .Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Disease>()
            .Property(d => d.RoboflowLabel)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<PredictionItem>()
            .Property(pi => pi.DetectedClass)
            .IsRequired()
            .HasMaxLength(200);
    }
}
