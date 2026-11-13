using eiupdds_backend.Models;

namespace eiupdds_backend.Repositories.Interfaces;

public interface IDiseaseRepository
{
    Task<IReadOnlyList<Disease>> GetActiveAsync(int? plantId, string? search);
    Task<Disease?> GetByIdAsync(int id);
    Task<Disease> AddAsync(Disease disease);
    Task UpdateAsync(Disease disease);
}
