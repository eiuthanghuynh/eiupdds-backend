namespace eiupdds_backend.Models;

public class Disease : BaseAuditableEntity
{
    public int PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RoboflowClassId { get; set; }
    public string RoboflowLabel { get; set; } = string.Empty;

    public Plant Plant { get; set; } = null!;
    public ICollection<PredictionItem> PredictionItems { get; set; } = new List<PredictionItem>();
}
