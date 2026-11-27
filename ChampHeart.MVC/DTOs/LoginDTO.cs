using System.ComponentModel.DataAnnotations;

namespace ChampHeart.MVC.DTOs;

// Sent to POST api/auth/token
public class LoginDTO
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
