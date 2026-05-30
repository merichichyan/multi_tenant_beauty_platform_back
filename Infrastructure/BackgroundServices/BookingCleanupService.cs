using Microsoft.EntityFrameworkCore;
using multi_tenant_beauty_platform_back.Infrastructure.Data;

namespace multi_tenant_beauty_platform_back.Infrastructure.BackgroundServices;

/// <summary>
/// Runs once every 24 hours and permanently deletes all bookings whose
/// <c>CreatedAt</c> timestamp is older than 3 months.
/// The deletion is silent — users simply no longer see the records.
/// </summary>
public sealed class BookingCleanupService : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(90); // ~3 months

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingCleanupService> _logger;

    public BookingCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once immediately at startup, then every 24 hours.
        while (!stoppingToken.IsCancellationRequested)
        {
            await PurgeOldBookingsAsync(stoppingToken);
            await Task.Delay(Period, stoppingToken);
        }
    }

    private async Task PurgeOldBookingsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow - RetentionWindow;

            var deleted = await db.Bookings
                .Where(b => b.CreatedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "BookingCleanup: removed {Count} booking(s) older than 3 months (cutoff: {Cutoff:u}).",
                    deleted, cutoff);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "BookingCleanup: error while purging old bookings.");
        }
    }
}
