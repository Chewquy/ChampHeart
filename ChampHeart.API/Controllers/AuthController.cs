using System.Security.Claims;
using ChampHeart.API.DTOs;
using ChampHeart.API.Models;
using ChampHeart.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDTO request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new{ errors = result.Errors.Select(e => e.Description)});
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Member");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return StatusCode(500, new{message = "Unable to assign user role."});
        }

        return StatusCode(201, new{message = "Account created successfully.",userId = user.Id});
    }

    // POST: api/auth/token
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDTO>> Login([FromBody] LoginDTO request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new{message = "Invalid email or password."});
        }

        var result = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return StatusCode(429, new{message = "Account temporarily locked. Try again later."});
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new{message = "Invalid email or password."});
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        return Ok(token);
    }

    // GET: api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new{userId = user.Id,email = user.Email,roles});
    }
}