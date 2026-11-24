using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChampHeart.API.DTOs;
using ChampHeart.API.Models;
using ChampHeart.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ChampHeart.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<TokenResponseDTO> GenerateTokenAsync(ApplicationUser user)
    {
        // Retrieve roles
        var roles = await _userManager.GetRolesAsync(user);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),

            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),

            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),

            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
        };

        // Add roles to claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Retrieve signing key
        var secret = _configuration["Jwt:Key"]?? throw new InvalidOperationException("JWT signing key is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

        // Set expiration 
        var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationMinutes", 60));

        // Generate JWT
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        // Return token and user info
        return new TokenResponseDTO
        {
            Token = new JwtSecurityTokenHandler()
                .WriteToken(token),
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Roles = roles,
                    ExpiresAt = expiresAt
        };
    }
}