using ChampHeart.MVC.DTOs;
using ChampHeart.MVC.Models;

namespace ChampHeart.MVC.Services.Interfaces;

public interface IAuthApiService
{
    Task<ApiResult<TokenResponseDTO>> LoginAsync(LoginDTO request);

    Task<ApiResult> RegisterAsync(RegisterDTO request);
}
