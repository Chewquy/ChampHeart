using System.ComponentModel.DataAnnotations;

namespace ChampHeart.API.DTOs;

public class CreateResourceDTO
{
    [Required(ErrorMessage = "Resource name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 100,
        ErrorMessage = "Capacity must be between 1 and 100.")]
    public int Capacity { get; set; } = 1;
}