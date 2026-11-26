using ChampHeart.API.Data;
using ChampHeart.API.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChampHeart.Tests.Helpers;

public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public TestDatabase()
    {
        // Create a temporary in-memory SQLite database
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);

        // Create all EF Core and Identity tables
        Context.Database.EnsureCreated();

        SeedData();
    }

    private void SeedData()
    {
        // Create test members
        Context.Users.AddRange(
            new ApplicationUser
            {
                Id = "member-1",
                UserName = "member1",
                NormalizedUserName = "MEMBER1",
                Email = "member1@test.local",
                NormalizedEmail = "MEMBER1@TEST.LOCAL"
            },
            new ApplicationUser
            {
                Id = "member-2",
                UserName = "member2",
                NormalizedUserName = "MEMBER2",
                Email = "member2@test.local",
                NormalizedEmail = "MEMBER2@TEST.LOCAL"
            }
        );

        // Create clinic resources
        Context.Resources.AddRange(
            new Resource
            {
                Id = 1,
                Name = "Consultation Room 1",
                Description = "General health consultations",
                Category = "General Consultation",
                Capacity = 1,
                IsActive = true
            },
            new Resource
            {
                Id = 2,
                Name = "Nursing Room 1",
                Description = "Nursing appointments",
                Category = "Nursing",
                Capacity = 1,
                IsActive = true
            },
            new Resource
            {
                Id = 3,
                Name = "Closed Consultation Room",
                Description = "Temporarily unavailable",
                Category = "General Consultation",
                Capacity = 1,
                IsActive = false
            }
        );

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
