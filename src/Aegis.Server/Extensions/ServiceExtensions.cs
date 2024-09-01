using Aegis.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Server.Extensions;

public static class ServiceExtensions
{
    public static void AddAegisServer(this IServiceCollection services)
    {
        services.AddHostedService<HeartbeatMonitor>();
        services.AddScoped<LicenseService>();
    }
}