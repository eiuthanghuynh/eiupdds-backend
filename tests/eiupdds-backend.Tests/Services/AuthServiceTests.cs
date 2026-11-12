using eiupdds_backend.DTOs.Auth;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace eiupdds_backend.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["JwtSettings:SecretKey"])
            .Returns("SuperSecretKeyForTestingJwtTokenGeneration123456!");
        _configMock.Setup(c => c["JwtSettings:Issuer"])
            .Returns("TestIssuer");
        _configMock.Setup(c => c["JwtSettings:Audience"])
            .Returns("TestAudience");
        _configMock.Setup(c => c["JwtSettings:ExpiryMinutes"])
            .Returns("60");

        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _configMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenValidRequest_ShouldReturnAuthResponse()
    {
        var request = new RegisterRequestDto
        {
            FullName = "Nguyen Van A",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _roleManagerMock.Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowArgumentException()
    {
        var request = new RegisterRequestDto
        {
            FullName = "Nguyen Van A",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var existingUser = new ApplicationUser { Email = request.Email };
        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnAuthResponse()
    {
        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser
        {
            Id = "user-id-123",
            Email = request.Email,
            FullName = "Test User",
            IsActive = true,
            Role = "User"
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        var result = await _authService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowUnauthorizedAccessException()
    {
        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        var user = new ApplicationUser
        {
            Email = request.Email,
            IsActive = true
        };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request));
    }
}