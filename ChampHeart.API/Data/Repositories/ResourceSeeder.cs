using ChampHeart.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ChampHeart.API.Data;

public static class ResourceSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context)
    {
        var resources = new List<Resource>();

        AddResources(
            resources,
            "General Consultation",
            "Consultation Room",
            "Room for general medical consultations.",
            5);

        AddResources(
            resources,
            "Nursing",
            "Nursing Room",
            "Room for nursing services and routine care.",
            5);

        AddResources(
            resources,
            "Mental Health",
            "Counselling Room",
            "Private room for mental health consultations.",
            5);

        AddResources(
            resources,
            "Physiotherapy",
            "Physiotherapy Room",
            "Room for physical rehabilitation appointments.",
            5);

        AddResources(
            resources,
            "Nutrition",
            "Nutrition Room",
            "Room for nutrition and dietary consultations.",
            5);

        var existingNames = await context.Resources
            .Select(r => r.Name)
            .ToListAsync();

        var missingResources = resources
            .Where(r => !existingNames.Contains(
                r.Name,
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingResources.Count > 0)
        {
            context.Resources.AddRange(missingResources);

            await context.SaveChangesAsync();
        }
    }

    private static void AddResources(
        List<Resource> resources,
        string category,
        string namePrefix,
        string description,
        int count)
    {
        for (int i = 1; i <= count; i++)
        {
            resources.Add(new Resource
            {
                Name = $"{namePrefix} {i}",
                Category = category,
                Description = description,
                Capacity = 1,
                IsActive = true
            });
        }
    }
}