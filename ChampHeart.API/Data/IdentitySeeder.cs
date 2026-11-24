using ChampHeart.API.Models;
using Microsoft.AspNetCore.Identity;

namespace ChampHeart.API.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        string[] roles = { "Member", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));

                EnsureSucceeded(result);
            }
        }

        // Only create sample accounts when enabled
        if (!configuration.GetValue<bool>("Seed:Enabled"))
            return;

        var adminPassword = configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException(
                "Seed admin password is missing.");

        var memberPassword = configuration["Seed:MemberPassword"]
            ?? throw new InvalidOperationException(
                "Seed member password is missing.");

        await CreateUserAsync(userManager,"admin@champheart.test",adminPassword,"Admin");

        await CreateUserAsync(userManager,"member1@champheart.test",memberPassword,"Member");

        await CreateUserAsync(userManager,"member2@champheart.test",memberPassword,"Member");
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user != null)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                throw new InvalidOperationException($"Existing seed account {email} has an unexpected role.");
            }

            return;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        EnsureSucceeded(result);

        var roleResult = await userManager.AddToRoleAsync(user, role);

        EnsureSucceeded(roleResult);
    }

    private static void EnsureSucceeded(
        IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ",result.Errors.Select(e => e.Description)));
        }
    }
}