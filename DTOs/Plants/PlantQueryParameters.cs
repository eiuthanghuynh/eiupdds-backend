namespace eiupdds_backend.DTOs.Plants;

public class PlantQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } // "name" hoặc "createdat"
    public bool IsDescending { get; set; } = false;
}