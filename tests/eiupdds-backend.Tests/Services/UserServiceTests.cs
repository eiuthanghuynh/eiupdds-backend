using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Users;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class UserServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenSelfDeleting_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var admin = new ApplicationUser
        {
            Id = "admin-1",
            FullName = "Admin One",
            Email = "admin1@example.com",
            Role = "Admin",
            IsActive = true
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SoftDeleteUserAsync("admin-1", "admin-1")
        );

        Assert.Equal("Administrators cannot delete their own account.", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenDeletingLastAdmin_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var admin = new ApplicationUser
        {
            Id = "admin-1",
            FullName = "Admin One",
            Email = "admin1@example.com",
            Role = "Admin",
            IsActive = true
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SoftDeleteUserAsync("admin-1", "other-caller")
        );

        Assert.Equal("Cannot deactivate the only remaining administrator in the system.", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenMultipleAdmins_DeactivatesAdminSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var admin1 = new ApplicationUser
        {
            Id = "admin-1",
            FullName = "Admin One",
            Email = "admin1@example.com",
            Role = "Admin",
            IsActive = true
        };
        var admin2 = new ApplicationUser
        {
            Id = "admin-2",
            FullName = "Admin Two",
            Email = "admin2@example.com",
            Role = "Admin",
            IsActive = true
        };
        context.Users.AddRange(admin1, admin2);
        await context.SaveChangesAsync();

        var service = new UserService(context);

        var result = await service.SoftDeleteUserAsync("admin-2", "admin-1");

        Assert.True(result);
        var deactivatedAdmin = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == "admin-2");
        Assert.NotNull(deactivatedAdmin);
        Assert.False(deactivatedAdmin.IsActive);
        Assert.NotNull(deactivatedAdmin.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenTargetIsRegularUser_DeactivatesUserSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var admin = new ApplicationUser
        {
            Id = "admin-1",
            FullName = "Admin One",
            Email = "admin1@example.com",
            Role = "Admin",
            IsActive = true
        };
        var user = new ApplicationUser
        {
            Id = "user-1",
            FullName = "Regular User",
            Email = "user1@example.com",
            Role = "User",
            IsActive = true
        };
        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();

        var service = new UserService(context);

        var result = await service.SoftDeleteUserAsync("user-1", "admin-1");

        Assert.True(result);
        var deactivatedUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == "user-1");
        Assert.NotNull(deactivatedUser);
        Assert.False(deactivatedUser.IsActive);
        Assert.NotNull(deactivatedUser.DeletedAt);
        Assert.NotNull(deactivatedUser.UpdatedAt);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new UserService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.SoftDeleteUserAsync("non-existent-id", "admin-1")
        );
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserExists_ReturnsProfile()
    {
        using var context = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FullName = "John Doe",
            Email = "john@example.com",
            Role = "User",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserService(context);
        var profile = await service.GetProfileAsync("user-1");

        Assert.NotNull(profile);
        Assert.Equal("user-1", profile.Id);
        Assert.Equal("John Doe", profile.FullName);
        Assert.Equal("john@example.com", profile.Email);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserExists_UpdatesAndReturnsProfile()
    {
        using var context = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = "user-1",
            FullName = "Old Name",
            Email = "old@example.com",
            Role = "User",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserService(context);
        var request = new UpdateProfileDto
        {
            FullName = "Updated Name"
        };

        var profile = await service.UpdateProfileAsync("user-1", request);

        Assert.NotNull(profile);
        Assert.Equal("Updated Name", profile.FullName);
    }
}
