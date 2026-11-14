using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Auth;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace eiupdds_backend.Tests.Services;

public class AuthServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "SuperSecretKeyForTestingJwtTokenGeneration123456!" },
                { "JwtSettings:Issuer", "TestIssuer" },
                { "JwtSettings:Audience", "TestAudience" },
                { "JwtSettings:ExpiryMinutes", "60" }
            })
            .Build();

        _passwordHasher = new PasswordHasher<ApplicationUser>();
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_WhenValidRequest_ShouldReturnAuthResponse()
    {
        using var context = CreateInMemoryDbContext();
        var authService = new AuthService(context, _passwordHasher, _configuration);

        var request = new RegisterRequestDto
        {
            FullName = "Nguyen Van A",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        var result = await authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FullName, result.FullName);
        Assert.Equal("User", result.Role);
        Assert.False(string.IsNullOrEmpty(result.Token));

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(savedUser);
        Assert.Equal("User", savedUser.Role);
        Assert.True(savedUser.IsActive);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowArgumentException()
    {
        using var context = CreateInMemoryDbContext();
        context.Users.Add(new ApplicationUser
        {
            Id = "existing-user",
            Email = "existing@example.com",
            FullName = "Existing User",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _passwordHasher, _configuration);

        var request = new RegisterRequestDto
        {
            FullName = "Nguyen Van A",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnAuthResponse()
    {
        using var context = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = "user-id-123",
            Email = "test@example.com",
            FullName = "Test User",
            Role = "User",
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _passwordHasher, _configuration);

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var result = await authService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal("Test User", result.FullName);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowUnauthorizedAccessException()
    {
        using var context = CreateInMemoryDbContext();
        var user = new ApplicationUser
        {
            Id = "user-id-123",
            Email = "test@example.com",
            FullName = "Test User",
            Role = "User",
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "CorrectPassword!");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var authService = new AuthService(context, _passwordHasher, _configuration);

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(request));
    }
}
