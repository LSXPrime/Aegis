using System.IdentityModel.Tokens.Jwt;
using Aegis.Server.Controllers;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Entities;
using Aegis.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aegis.Server.Tests.Controllers;

public class AuthenticationControllerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthenticationController _controller;
    private readonly AuthService _authService;

    // Constructor for setting up the test environment
    public AuthenticationControllerTests()
    {
        // 1. Set up an in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        // 2. Configure JWT settings for testing
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "ThisIsMyVerySecretKeyForJWT!",
            Salt = "SaltForPasswordHashing",
            AccessTokenExpirationInDays = 1,
            RefreshTokenExpirationInDays = 7
        });

        // 3. Create instances of AuthService and AuthenticationController for testing
        _authService = new AuthService(_dbContext, jwtSettings);
        _controller = new AuthenticationController(_authService, _dbContext);
    }

    #region Register Tests

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "testpassword",
            ConfirmPassword = "testpassword",
            FullName = "Test User",
            Role = "User"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User registered successfully.", okResult.Value);

        // Verify user is saved in the database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == registerDto.Username);
        Assert.NotNull(user);
        Assert.Equal(registerDto.Email, user.Email);
        Assert.True(_authService.VerifyPassword(registerDto.Password, user.PasswordHash));
    }

    [Fact]
    public async Task Register_ExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User { Username = "existinguser", Email = "existing@example.com", PasswordHash = _authService.HashPassword("password") };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var registerDto = new RegisterDto { Username = "existinguser", Email = "newuser@example.com", Password = "newpassword", ConfirmPassword = "newpassword" };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username or email is already taken.", badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User { Username = "existinguser", Email = "existing@example.com", PasswordHash = _authService.HashPassword("password") };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var registerDto = new RegisterDto { Username = "newuser", Email = "existing@example.com", Password = "newpassword", ConfirmPassword = "newpassword" };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username or email is already taken.", badRequestResult.Value);
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto { Username = "testuser", Email = "invalid email", Password = "testpassword", ConfirmPassword = "testpassword", FullName = "Test User", Role = "User" };
        _controller.ModelState.AddModelError("Email", "Invalid email format.");

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResultWithJwtToken()
    {
        // Arrange
        var user = new User { Username = "testuser", PasswordHash = _authService.HashPassword("testpassword"), Role = "User" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Username = "testuser", Password = "testpassword" };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var jwtTokenDto = Assert.IsType<JwtTokenDto>(okResult.Value);
        Assert.NotNull(jwtTokenDto);
        Assert.NotEmpty(jwtTokenDto.AccessToken);
        Assert.NotEmpty(jwtTokenDto.RefreshToken);

        // Verify token claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.ReadToken(jwtTokenDto.AccessToken) as JwtSecurityToken;
        Assert.Equal(user.Id.ToString(), securityToken?.Claims.First(claim => claim.Type == "nameid").Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User { Username = "testuser", PasswordHash = _authService.HashPassword("testpassword"), Role = "User" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Username = "testuser", Password = "wrongpassword" };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid username or password.", unauthorizedResult.Value);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ReturnsOkResultWithNewTokens()
    {
        // Arrange
        var user = new User { Username = "testuser", PasswordHash = _authService.HashPassword("testpassword"), Role = "User" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Generate initial tokens
        var initialTokens = _authService.GenerateJwtToken(user.Id, user.Username, user.Role);
        var refreshToken = new RefreshTokenDto { Token = initialTokens.RefreshToken };

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var newTokens = Assert.IsType<JwtTokenDto>(okResult.Value);
        Assert.NotNull(newTokens);
        Assert.NotEmpty(newTokens.AccessToken);
        Assert.NotEmpty(newTokens.RefreshToken);
        Assert.NotEqual(initialTokens.RefreshToken, newTokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshToken = new RefreshTokenDto { Token = "invalid-refresh-token" };

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid refresh token.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task RefreshToken_MissingUser_ReturnsUnauthorized()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto { Token = "some-refresh-token" };
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = Guid.NewGuid(), // Non-existing user
            Token = refreshTokenDto.Token,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.RefreshToken(refreshTokenDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User associated with refresh token not found.", unauthorizedResult.Value);
    }
    
    #endregion 
}