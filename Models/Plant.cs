namespace eiupdds_backend.Models;

public class Plant : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Disease> Diseases { get; set; } = new List<Disease>();
    public ICollection<PredictionHistory> PredictionHistories { get; set; } = new List<PredictionHistory>();
}
