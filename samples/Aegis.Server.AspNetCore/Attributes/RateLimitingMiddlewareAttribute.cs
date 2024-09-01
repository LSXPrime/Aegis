using Aegis.Server.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Aegis.Server.AspNetCore.Attributes;

/// <summary>
///     Attribute for limiting access to an action.
/// </summary>
/// <param name="limit">The maximum number of requests allowed within the specified time period.</param>
/// <param name="period">The time period within which the limit applies.</param>
public class RateLimitingMiddlewareAttribute(int limit, string period) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var memoryCache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var middleware =
            new RateLimitingMiddleware(async _ => await next(), memoryCache, limit, TimeSpan.Parse(period));
        await middleware.InvokeAsync(httpContext);
    }
}