namespace eiupdds_backend.Services.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadImageAsync(IFormFile file);
    bool DeleteImage(string relativePath);
}
