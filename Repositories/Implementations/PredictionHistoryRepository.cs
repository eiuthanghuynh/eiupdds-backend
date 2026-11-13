using eiupdds_backend.Data;
using eiupdds_backend.Models;
using eiupdds_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Repositories.Implementations;

public class PredictionHistoryRepository : IPredictionHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PredictionHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<PredictionHistory> Items, int TotalCount)> GetByUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _dbContext.PredictionHistories
            .AsNoTracking()
            .Include(h => h.Plant)
            .Include(h => h.PredictionItems)
            .Where(h => h.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PredictionHistory?> GetByIdForUserAsync(int id, string userId)
    {
        return await _dbContext.PredictionHistories
            .Include(h => h.Plant)
            .Include(h => h.PredictionItems)
                .ThenInclude(i => i.Disease)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
    }

    public async Task DeleteAsync(PredictionHistory history)
    {
        _dbContext.PredictionHistories.Remove(history);
        await _dbContext.SaveChangesAsync();
    }
}
