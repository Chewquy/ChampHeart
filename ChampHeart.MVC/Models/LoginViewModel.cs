using System.ComponentModel.DataAnnotations;

namespace ChampHeart.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Page to go back to after logging in
    public string? ReturnUrl { get; set; }
}
