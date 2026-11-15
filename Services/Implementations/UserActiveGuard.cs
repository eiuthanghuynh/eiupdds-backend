using eiupdds_backend.Data;
using eiupdds_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Services.Implementations;

public class UserActiveGuard : IUserActiveGuard
{
    private readonly ApplicationDbContext _context;

    public UserActiveGuard(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsUserActiveAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Id == userId && u.IsActive);
    }
}
