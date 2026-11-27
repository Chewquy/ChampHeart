using System.Security.Claims;
using ChampHeart.MVC.DTOs;
using ChampHeart.MVC.Models;
using ChampHeart.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChampHeart.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthApiService _authApiService;

    public AccountController(IAuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    // GET: /Account/Login
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Get a JWT from the API
        var result = await _authApiService.LoginAsync(new LoginDTO
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError("", result.ErrorMessage);
            return View(model);
        }

        var token = result.Data;

        // Keep the JWT in session for the next API calls
        new ChampHeartSession(HttpContext.Session).SetToken(token);

        // Sign in to the MVC with the user info and roles from the token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, token.UserId),
            new Claim(ClaimTypes.Name, token.Email),
            new Claim(ClaimTypes.Email, token.Email)
        };

        foreach (var role in token.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                ExpiresUtc = token.ExpiresAt
            });

        TempData["SuccessMessage"] = $"Welcome, {token.Email}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // POST: /Account/Logout
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        new ChampHeartSession(HttpContext.Session).RemoveToken();

        TempData["SuccessMessage"] = "You have been logged out.";

        return RedirectToAction("Index", "Home");
    }
}
