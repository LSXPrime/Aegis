using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Entities;
using Aegis.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aegis.Server.Tests.Services;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "testsecret",
            Salt = "testsalt",
            AccessTokenExpirationInDays = 1,
            RefreshTokenExpirationInDays = 7
        });

        _authService = new AuthService(_dbContext, _jwtSettings);
    }

    #region LoginUserAsync Tests

    [Fact]
    public async Task LoginUserAsync_ValidCredentials_ReturnsJwtTokenDto()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = _authService.HashPassword("testpassword"), // Hash the password for testing
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LoginUserAsync(new LoginDto { Username = "testuser", Password = "testpassword" });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.AccessTokenExpiration > DateTime.UtcNow);
        Assert.True(result.RefreshTokenExpiration > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginUserAsync_InvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginUserAsync(new LoginDto { Username = "nonexistentuser", Password = "testpassword" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginUserAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = _authService.HashPassword("testpassword"),
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LoginUserAsync(new LoginDto { Username = "testuser", Password = "wrongpassword" });

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_UniqueCredentials_ReturnsTrueAndSavesUser()
    {
        // Arrange
        var newUser = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "newpassword",
            ConfirmPassword = "newpassword",
            FullName = "New User",
            Role = "User"
        };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        Assert.True(result);
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == newUser.Username);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task RegisterAsync_ExistingUsername_ReturnsFalse()
    {
        // Arrange
        var existingUser = new User { Username = "existinguser", Email = "existing@example.com" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var newUser = new RegisterDto { Username = "existinguser", Email = "newuser@example.com" };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFalse()
    {
        // Arrange
        var existingUser = new User { Username = "existinguser", Email = "existing@example.com" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var newUser = new RegisterDto { Username = "newuser", Email = "existing@example.com" };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GenerateJwtToken Tests

    [Fact]
    public async Task GenerateJwtToken_ReturnsValidJwtTokenDto()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _authService.GenerateJwtToken(userId, "testuser", "User");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.True(result.AccessTokenExpiration > DateTime.UtcNow);
        Assert.True(result.RefreshTokenExpiration > DateTime.UtcNow);

        // Verify token claims
        var claims = await _authService.ValidateTokenAsync(result.AccessToken);
        Assert.Equal(userId.ToString(), claims?.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("testuser", claims?.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("User", claims?.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task GenerateJwtToken_SavesRefreshTokenToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        _authService.GenerateJwtToken(userId, "testuser", "User");

        // Assert
        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        Assert.NotNull(refreshToken);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueToken()
    {
        // Act
        var token1 = _authService.GenerateRefreshToken();
        var token2 = _authService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = _authService.GenerateJwtToken(userId, "testuser", "User").AccessToken;

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId.ToString(), result.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("testuser", result.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("User", result.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ThrowsException()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Value.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }),
            Expires = DateTime.UtcNow.AddMinutes(-1), // Expired token
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await _authService.ValidateTokenAsync(tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor))));
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        const string invalidToken = "invalidtoken";

        // Act
        var result = await _authService.ValidateTokenAsync(invalidToken);

        // Assert
        Assert.Null(result);
    }

    #endregion
}