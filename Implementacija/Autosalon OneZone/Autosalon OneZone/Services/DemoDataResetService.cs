using Autosalon_OneZone.Data;
using Microsoft.Extensions.Options;

namespace Autosalon_OneZone.Services;

public sealed class DemoOptions
{
    public bool Enabled { get; set; }
    public bool ResetEnabled { get; set; }
    public int ResetIntervalMinutes { get; set; } = 60;
}

public sealed class DemoResetSchedule
{
    private long _nextResetUtcTicks;

    public DateTime? NextResetUtc
    {
        get
        {
            var ticks = Interlocked.Read(ref _nextResetUtcTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public void ScheduleAfter(TimeSpan interval) =>
        Interlocked.Exchange(ref _nextResetUtcTicks, DateTime.UtcNow.Add(interval).Ticks);
}

public interface IDemoDataResetService
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDataResetService : IDemoDataResetService
{
    private static readonly SemaphoreSlim ResetLock = new(1, 1);
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly DemoResetSchedule _schedule;
    private readonly DemoOptions _options;
    private readonly ILogger<DemoDataResetService> _logger;

    public DemoDataResetService(
        IServiceProvider services,
        IConfiguration configuration,
        DemoResetSchedule schedule,
        IOptions<DemoOptions> options,
        ILogger<DemoDataResetService> logger)
    {
        _services = services;
        _configuration = configuration;
        _schedule = schedule;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await ResetLock.WaitAsync(cancellationToken);
        try
        {
            await DatabaseInitializer.ResetDemoDataAsync(
                _services,
                _configuration,
                _logger,
                cancellationToken);

            _schedule.ScheduleAfter(TimeSpan.FromMinutes(_options.ResetIntervalMinutes));
        }
        finally
        {
            ResetLock.Release();
        }
    }
}

public sealed class DemoDataResetBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DemoResetSchedule _schedule;
    private readonly DemoOptions _options;
    private readonly ILogger<DemoDataResetBackgroundService> _logger;

    public DemoDataResetBackgroundService(
        IServiceScopeFactory scopeFactory,
        DemoResetSchedule schedule,
        IOptions<DemoOptions> options,
        ILogger<DemoDataResetBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedule = schedule;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ResetEnabled)
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(_options.ResetIntervalMinutes);
        _schedule.ScheduleAfter(interval);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider
                    .GetRequiredService<IDemoDataResetService>()
                    .ResetAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Periodic demo data reset failed. It will be retried at the next interval.");
                _schedule.ScheduleAfter(interval);
            }
        }
    }
}
