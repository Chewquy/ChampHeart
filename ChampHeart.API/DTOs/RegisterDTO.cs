using System.ComponentModel.DataAnnotations;

namespace ChampHeart.API.DTOs;

public class RegisterDTO
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage =
        "Password must contain at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password),
        ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}