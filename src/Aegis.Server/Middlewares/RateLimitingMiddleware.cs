using Microsoft.Extensions.Caching.Memory;

namespace Aegis.Server.Middlewares;

public class RateLimitingMiddleware(RequestDelegate next, IMemoryCache memoryCache, int limit, TimeSpan period)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress;
        var key = $"RateLimit-{ipAddress}";

        if (memoryCache.TryGetValue(key, out int requestCount))
        {
            if (requestCount >= limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded.");
                return;
            }

            memoryCache.Set(key, requestCount + 1, period);
        }
        else
        {
            memoryCache.Set(key, 1, period);
        }

        await next(context);
    }
}