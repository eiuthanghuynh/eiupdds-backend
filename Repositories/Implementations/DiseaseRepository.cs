using eiupdds_backend.Data;
using eiupdds_backend.Models;
using eiupdds_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eiupdds_backend.Repositories.Implementations;

public class DiseaseRepository : IDiseaseRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DiseaseRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Disease>> GetActiveAsync(int? plantId, string? search)
    {
        var query = _dbContext.Diseases
            .AsNoTracking()
            .Include(d => d.Plant)
            .AsQueryable();

        if (plantId.HasValue)
        {
            query = query.Where(d => d.PlantId == plantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term) || d.RoboflowLabel.Contains(term));
        }

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Disease?> GetByIdAsync(int id)
    {
        return await _dbContext.Diseases
            .Include(d => d.Plant)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Disease> AddAsync(Disease disease)
    {
        _dbContext.Diseases.Add(disease);
        await _dbContext.SaveChangesAsync();
        return disease;
    }

    public async Task UpdateAsync(Disease disease)
    {
        _dbContext.Diseases.Update(disease);
        await _dbContext.SaveChangesAsync();
    }
}
