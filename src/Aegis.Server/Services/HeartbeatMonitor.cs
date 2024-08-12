using Aegis.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Services;

public class HeartbeatMonitor(ApplicationDbContext dbContext) : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Starts the background task to monitor and clean up expired activations.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(Monitor, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); 
        return Task.CompletedTask;
    }

    /// <summary>
    /// Monitors the activations and removes any that have expired.
    /// </summary>
    /// <param name="state">An object that contains state information for this member.</param>
    private async void Monitor(object? state)
    {
        var timeoutThreshold = DateTime.UtcNow.Subtract(_heartbeatTimeout);

        var expiredActivations = await dbContext.Activations
            .Where(a => a.LastHeartbeat < timeoutThreshold)
            .ToListAsync();

        dbContext.Activations.RemoveRange(expiredActivations);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Stops the background task.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the timer resource.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}