using System.Net.Http.Json;
using ChampHeart.MVC.DTOs;
using ChampHeart.MVC.Models;
using ChampHeart.MVC.Services.Interfaces;

namespace ChampHeart.MVC.Services;

public class AuthApiService : ApiServiceBase, IAuthApiService
{
    public AuthApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        : base(httpClient, httpContextAccessor)
    {
    }

    // POST: api/auth/token
    public async Task<ApiResult<TokenResponseDTO>> LoginAsync(LoginDTO request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/token", request);

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<TokenResponseDTO>.Fail(
                await ReadErrorAsync(response), (int)response.StatusCode);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponseDTO>();

        if (token == null)
        {
            return ApiResult<TokenResponseDTO>.Fail("Invalid response from the server.", 500);
        }

        return ApiResult<TokenResponseDTO>.Ok(token);
    }

    // POST: api/auth/register
    public async Task<ApiResult> RegisterAsync(RegisterDTO request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Fail(
                await ReadErrorAsync(response), (int)response.StatusCode);
        }

        return ApiResult.Ok();
    }
}
