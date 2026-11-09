using eiupdds_backend.DTOs.Roboflow;

namespace eiupdds_backend.Services.Interfaces;

public interface IRoboflowService
{
    Task<RoboflowModelOutput> DetectDiseaseAsync(string imagePhysicalPath);
}
