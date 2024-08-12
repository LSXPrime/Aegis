using System.Security.Claims;
using Aegis.Server.Data;
using Aegis.Server.Services;
using Microsoft.EntityFrameworkCore;


namespace Aegis.Server.Middlewares;

/// <summary>
/// Middleware for handling authorization, validating JWT tokens and checking user permissions.
/// </summary>
public class AuthorizationMiddleware(
    RequestDelegate next,
    AuthService authService,
    ApplicationDbContext dbContext,
    string[] roles,
    bool allowAnonymous = false)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.User = new ClaimsPrincipal();

        if (await TryAuthenticateWithJwt(context)) 
        {
            // JWT authentication successful, proceed to authorization
            await AuthorizeUserAsync(context, roles); 
            await next(context);
            return;
        }

        if (TryAuthenticateWithApiKey(context))
        {
            // API key authentication successful, proceed to authorization
            await AuthorizeUserAsync(context, roles); 
            await next(context);
            return; 
        }

        // No valid authentication (JWT or API key), handle based on allowAnonymous
        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized: Missing or invalid credentials.");
    }

    internal async Task<bool> TryAuthenticateWithJwt(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return false; 
        }

        var token = authorizationHeader.ToString().Split(' ')[1];
        var claimsPrincipal = await authService.ValidateTokenAsync(token);
        if (claimsPrincipal == null)
        {
            return false;
        }

        context.User = claimsPrincipal;
        return true;
    }

    internal bool TryAuthenticateWithApiKey(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyHeader))
        {
            return false; 
        }

        // 1. Retrieve the user associated with the API key from your database.
        var user = dbContext.Users.FirstOrDefault(u => u.ApiKey == apiKeyHeader.ToString()); 

        if (user == null)
        {
            return false;
        }

        // 2. Create a ClaimsIdentity and ClaimsPrincipal for the authenticated user.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        return true;
    }

    internal async Task<bool> AuthorizeUserAsync(HttpContext context, string[] allowedRoles)
    {
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (roleClaim == null || 
            !allowedRoles.Contains(roleClaim, StringComparer.OrdinalIgnoreCase) ||
            !await dbContext.Roles.AnyAsync(x => x.Name == roleClaim))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: Insufficient permissions.");
            return false; 
        }

        var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
        if (!await dbContext.Users.AnyAsync(x => x.Username == userName))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: User not found.");
            return false; 
        }

        return true;
    }
}