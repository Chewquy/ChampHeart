namespace ChampHeart.MVC.DTOs;

// Returned by GET api/resources and api/admin/resources
public class ResourceResponseDTO
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; }
}
