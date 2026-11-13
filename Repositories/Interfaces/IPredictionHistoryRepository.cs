using eiupdds_backend.Models;

namespace eiupdds_backend.Repositories.Interfaces;

public interface IPredictionHistoryRepository
{
    Task<(IReadOnlyList<PredictionHistory> Items, int TotalCount)> GetByUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate);

    Task<PredictionHistory?> GetByIdForUserAsync(int id, string userId);
    Task DeleteAsync(PredictionHistory history);
}
