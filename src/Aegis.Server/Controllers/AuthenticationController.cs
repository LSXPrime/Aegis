using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(AuthService authService, ApplicationDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="newUser">The user registration details.</param>
    /// <returns>
    /// Returns an OK response with a message "User registered successfully." if the registration is successful.
    /// Returns a BadRequest response with a message "Username or email is already taken." if the username or email is already taken.
    /// Returns a BadRequest response with the ModelState if the request is invalid.
    /// </returns>
    [HttpPost("register")] 
    public async Task<IActionResult> Register([FromBody] RegisterDto newUser)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var result = await authService.RegisterAsync(newUser);
        return result ? Ok("User registered successfully.") : BadRequest("Username or email is already taken.");
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    /// <param name="login">The user login details.</param>
    /// <returns>
    /// Returns an OK response with the generated jwt token if the login is successful.
    /// Returns an Unauthorized response with a message "Invalid username or password." if the login fails.
    /// Returns a BadRequest response with the ModelState if the request is invalid.
    /// </returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var token = await authService.LoginUserAsync(login);
        return token != null ? Ok(token) : Unauthorized("Invalid username or password.");
    }
    
    /// <summary>
    /// Refreshes the JWT token using the provided refresh token.
    /// </summary>
    /// <param name="refreshTokenDto">The refresh token to use for token refresh.</param>
    /// <returns>
    /// Returns an OK response with the new JWT token if the refresh is successful.
    /// Returns an Unauthorized response with a message "Invalid refresh token." if the refresh token is invalid.
    /// Returns an Unauthorized response with a message "User associated with refresh token not found." if the user associated with the refresh token is not found.
    /// </returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var storedRefreshToken = await dbContext.RefreshTokens.Where(r => r.Token == refreshTokenDto.Token).FirstOrDefaultAsync();
        if (storedRefreshToken == null || storedRefreshToken.Expires < DateTime.UtcNow)
            return Unauthorized("Invalid refresh token.");
        
        dbContext.RefreshTokens.Remove(storedRefreshToken);
        await dbContext.SaveChangesAsync();

        var userId = storedRefreshToken.UserId; 
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null) 
            return Unauthorized("User associated with refresh token not found.");

        var newTokens = authService.GenerateJwtToken(userId, user.Username, storedRefreshToken.Role);
        
        return Ok(newTokens);
    }
}