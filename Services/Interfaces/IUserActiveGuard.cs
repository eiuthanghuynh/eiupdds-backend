namespace eiupdds_backend.Services.Interfaces;

public interface IUserActiveGuard
{
    Task<bool> IsUserActiveAsync(string userId);
}
