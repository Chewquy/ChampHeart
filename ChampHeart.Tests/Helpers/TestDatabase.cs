using ChampHeart.API.Data;
using ChampHeart.API.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChampHeart.Tests.Helpers
{
    public class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext Context { get; }

        public TestDatabase()
        {
            // Temporary database stored in memory
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            _connection.Open();

            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            Context = new ApplicationDbContext(options);

            // Create tables for testing
            Context.Database.EnsureCreated();

            SeedData();
        }

        private void SeedData()
        {
            // Test users
            Context.Users.AddRange(
                new ApplicationUser
                {
                    Id = "member-1",
                    UserName = "member1"
                },
                new ApplicationUser
                {
                    Id = "member-2",
                    UserName = "member2"
                }
            );

            // Test resources
            Context.Resources.AddRange(
                new Resource
                {
                    Id = 1,
                    Name = "Consultation Room 1",
                    Category = "General Consultation",
                    Capacity = 1,
                    IsActive = true
                },
                new Resource
                {
                    Id = 2,
                    Name = "Nursing Room 1",
                    Category = "Nursing",
                    Capacity = 1,
                    IsActive = true
                },
                new Resource
                {
                    Id = 3,
                    Name = "Closed Room",
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
}
