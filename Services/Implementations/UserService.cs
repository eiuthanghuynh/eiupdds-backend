using eiupdds_backend.DTOs.Users;
using eiupdds_backend.Models;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eiupdds_backend.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new KeyNotFoundException("User not found or has been deactivated.");
            }
            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
        public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new KeyNotFoundException("User not found or has been deactivated.");
            }

            user.FullName = request.FullName;
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException($"Failed to update profile: {errors}");
            }
            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
        public async Task<bool> SoftDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new KeyNotFoundException("User not found or already deactivated.");
            }

            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException($"Failed to delete user: {errors}");
            }
            return true;
        }
    }
}
