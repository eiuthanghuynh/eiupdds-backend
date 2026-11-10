using eiupdds_backend.DTOs.Roboflow;

namespace eiupdds_backend.Services.Interfaces;

/// <summary>
/// Service contract for running plant disease inference via Roboflow Serverless Workflow.
/// </summary>
public interface IRoboflowService
{
    /// <summary>
    /// Sends an image file on disk to Roboflow Workflow and returns parsed bounding box predictions and image metadata.
    /// </summary>
    /// <param name="imagePhysicalPath">Full physical disk path to the image file.</param>
    /// <returns>Model output containing image dimensions and detected disease predictions.</returns>
    Task<RoboflowModelOutput> DetectDiseaseAsync(string imagePhysicalPath);
}
