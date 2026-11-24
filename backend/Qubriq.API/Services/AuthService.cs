using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Auth;
using Qubriq.API.Models;

namespace Qubriq.API.Services;

public class AuthService
{
    private readonly QubriqDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        QubriqDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _db.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var roles = user.UserRoles
            .Where(x => x.Role != null)
            .Select(x => x.Role.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return CreateTokenResponse(user, roles);
    }

    private AuthResponse CreateTokenResponse(
        User user,
        List<string> roles)
    {
        var signingKeyValue = _configuration["Jwt:SigningKey"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(signingKeyValue) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is incomplete.");
        }

        var keyBytes = Convert.FromBase64String(signingKeyValue);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        var expiryMinutes = 30;
        var configuredExpiry = _configuration["Jwt:ExpiryMinutes"];

        if (!string.IsNullOrWhiteSpace(configuredExpiry))
        {
            if (!int.TryParse(configuredExpiry, out expiryMinutes) ||
                expiryMinutes < 1 ||
                expiryMinutes > 1440)
            {
                throw new InvalidOperationException(
                    "Jwt:ExpiryMinutes must be between 1 and 1440.");
            }
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (roles.Contains("ShipmentRecipient") &&
            !string.IsNullOrWhiteSpace(user.RecipientType))
        {
            claims.Add(new Claim("recipient_type", user.RecipientType));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AuthResponse
        {
            AccessToken = tokenString,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAt,
            User = new AuthenticatedUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                RecipientType = roles.Contains("ShipmentRecipient")
                    ? user.RecipientType
                    : null
            }
        };
    }
}
