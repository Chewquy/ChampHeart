namespace ChampHeart.MVC.DTOs;

// Returned by POST api/auth/token
public class TokenResponseDTO
{
    public string Token { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IList<string> Roles { get; set; }
        = new List<string>();

    public DateTime ExpiresAt { get; set; }
}
