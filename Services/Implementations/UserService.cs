using eiupdds_backend.Data;
using eiupdds_backend.DTOs.Users;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive)
        {
            throw new KeyNotFoundException("User not found or has been deactivated.");
        }

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive)
        {
            throw new KeyNotFoundException("User not found or has been deactivated.");
        }

        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<bool> SoftDeleteUserAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive)
        {
            throw new KeyNotFoundException("User not found or already deactivated.");
        }

        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
