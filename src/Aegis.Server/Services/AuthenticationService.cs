using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Entities;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[assembly: InternalsVisibleTo("Aegis.Server.Tests")]
namespace Aegis.Server.Services;

public class AuthService(ApplicationDbContext dbContext, IOptions<JwtSettings> options)
{
    /// <summary>
    /// Authenticates a user and generates a JWT token if successful.
    /// </summary>
    /// <param name="login">The login credentials.</param>
    /// <returns>The JWT token DTO containing the access token, and expiration time, or null if authentication failed.</returns>
    public async Task<JwtTokenDto?> LoginUserAsync(LoginDto login)
    {
        var user = await dbContext.Users.Where(x => x.Username == login.Username).FirstOrDefaultAsync();
        if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user.Id, user.Username, user.Role);

        return token;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="newUser">The new user registration details.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    public async Task<bool> RegisterAsync(RegisterDto newUser)
    {
        if (newUser.Password != newUser.ConfirmPassword || await dbContext.Users.AnyAsync(x => x.Username == newUser.Username) || await dbContext.Users.AnyAsync(x => x.Email == newUser.Email))
        {
            return false;
        }

        var user = new User
        {
            Username = newUser.Username,
            Email = newUser.Email,
            PasswordHash = HashPassword(newUser.Password),
            Role = newUser.Role,
            FullName = newUser.FullName,
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Generates a JWT and refresh token for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="role">The role of the user.</param>
    /// <returns>The JWT token DTO containing the access token, refresh token, and expiration times.</returns>
    public JwtTokenDto GenerateJwtToken(Guid userId, string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = SHA256.HashData(Encoding.ASCII.GetBytes(options.Value.Secret));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddDays(options.Value.AccessTokenExpirationInDays),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        var refreshExpirationDate = DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationInDays);

         dbContext.RefreshTokens.Add(new RefreshToken
            { UserId = userId, Token = refreshToken, Expires = refreshExpirationDate, Role = role });

        dbContext.SaveChanges();
        
        return new JwtTokenDto
        {
            AccessToken = tokenHandler.WriteToken(accessToken),
            AccessTokenExpiration = tokenDescriptor.Expires.Value,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpirationDate
        };
    }

    /// <summary>
    /// Generates a random refresh token.
    /// </summary>
    /// <returns>The generated refresh token.</returns>
    internal string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT token and returns the associated ClaimsPrincipal if valid.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The ClaimsPrincipal associated with the token, or null if the token is invalid.</returns>
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = SHA256.HashData(Encoding.ASCII.GetBytes(options.Value.Secret));
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            }, out _);

            return Task.FromResult(claimsPrincipal)!;
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal>(null!)!;
        }
    }

    /// <summary>
    /// Hashes a password using PBKDF2 with HMACSHA256.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password as a base64-encoded string.</returns>
    internal string HashPassword(string password)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: Encoding.ASCII.GetBytes(options.Value.Salt),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
    }

    /// <summary>
    /// Verifies a password against a hashed password.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the passwords match, false otherwise.</returns>
    internal bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }
}