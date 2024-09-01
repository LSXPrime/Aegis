using Aegis.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aegis.Server.Services;

public class HeartbeatMonitor(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(10);
    private readonly IServiceScopeFactory _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

    /// <summary>
    ///     Starts the background task to monitor and clean up expired activations.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Monitor();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    /// <summary>
    ///     Monitors the activations and removes any that have expired.
    /// </summary>
    private async Task Monitor()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

        var timeoutThreshold = DateTime.UtcNow.Subtract(_heartbeatTimeout);

        var expiredActivations = await dbContext.Activations
            .Where(a => a.LastHeartbeat < timeoutThreshold)
            .ToListAsync();

        dbContext.Activations.RemoveRange(expiredActivations);
        await dbContext.SaveChangesAsync();
    }
}