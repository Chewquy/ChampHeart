using ChampHeart.API.DTOs;
using ChampHeart.API.Models;

namespace ChampHeart.API.Services.Interfaces;

public interface ITokenService
{
    Task<TokenResponseDTO> GenerateTokenAsync(
        ApplicationUser user);
}