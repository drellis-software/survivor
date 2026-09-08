using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Models;
using BTSurvivorPool.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BTSurvivorPool.Tests;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-must-be-32-chars!!",
                ["JwtSettings:ExpiryHours"] = "8",
                ["JwtSettings:Issuer"] = "BTSurvivorPool",
                ["JwtSettings:Audience"] = "BTSurvivorPool"
            })
            .Build();

    [Fact]
    public async Task Login_WithWrongPin_ThrowsUnauthorized()
    {
        var db = CreateDb();
        var config = CreateConfig();

        // Register a user
        var service = new AuthService(db, config);
        await service.RegisterAsync(new RegisterDto
        {
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            Pin = "1234"
        });

        // Try to login with wrong PIN
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginDto { Username = "testuser", Pin = "9999" }));
    }

    [Fact]
    public async Task Register_FirstUser_GetsSuperAdminRole()
    {
        var db = CreateDb();
        var config = CreateConfig();
        var service = new AuthService(db, config);

        var result = await service.RegisterAsync(new RegisterDto
        {
            Username = "admin",
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@test.com",
            Pin = "123456"
        });

        Assert.Equal("SuperAdmin", result.Role);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsInvalidOperation()
    {
        var db = CreateDb();
        var config = CreateConfig();
        var service = new AuthService(db, config);

        await service.RegisterAsync(new RegisterDto { Username = "dupeuser", FirstName = "A", LastName = "B", Email = "a@a.com", Pin = "1234" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDto { Username = "dupeuser", FirstName = "C", LastName = "D", Email = "c@c.com", Pin = "5678" }));
    }
}
