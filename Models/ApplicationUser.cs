using Microsoft.AspNetCore.Identity;

namespace eiupdds_backend.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PredictionHistory> PredictionHistories { get; set; } = new List<PredictionHistory>();
}
