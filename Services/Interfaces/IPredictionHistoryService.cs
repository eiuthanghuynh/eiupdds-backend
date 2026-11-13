using eiupdds_backend.DTOs.Common;
using eiupdds_backend.DTOs.Diagnosis;

namespace eiupdds_backend.Services.Interfaces;

public interface IPredictionHistoryService
{
    Task<PagedResultDto<DiagnosisHistoryListItemDto>> GetHistoryAsync(
        string userId,
        int pageNumber,
        int pageSize,
        DateTime? fromDate,
        DateTime? toDate);

    Task<DiagnosisHistoryDetailDto> GetByIdAsync(int id, string userId);
    Task DeleteAsync(int id, string userId);
}
