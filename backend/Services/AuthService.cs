using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BTSurvivorPool.Services;

public class AuthService(ApplicationDbContext db, IConfiguration config) : IAuthService
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("Username already taken.");

        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already registered.");

        var isFirst = !await db.Users.AnyAsync();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin),
            Role = isFirst ? UserRole.SuperAdmin : UserRole.Player,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GenerateToken(user),
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username)
            ?? throw new UnauthorizedAccessException("Invalid username or PIN.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Pin, user.PinHash))
            throw new UnauthorizedAccessException("Invalid username or PIN.");

        return new AuthResponseDto
        {
            Token = GenerateToken(user),
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }

    private string GenerateToken(User user)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var expiryHours = int.Parse(jwtSettings["ExpiryHours"] ?? "8");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
