using eiupdds_backend.DTOs.Users;

namespace eiupdds_backend.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto request);
        Task<bool> SoftDeleteUserAsync(string userId);
    }
}
