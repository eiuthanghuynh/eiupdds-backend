namespace eiupdds_backend.Models;

public class PredictionHistory
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public int? PlantId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string? RawResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public Plant? Plant { get; set; }
    public ICollection<PredictionItem> PredictionItems { get; set; } = new List<PredictionItem>();
}
