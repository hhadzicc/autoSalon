using Autosalon_OneZone.Data;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public sealed class SupportEmailOutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SupportEmailOutboxBackgroundService> _logger;

    public SupportEmailOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SupportEmailOutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Support email outbox processing failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;
        var messages = await context.EmailPodrskeOutbox
            .Include(entry => entry.Poruka)
                .ThenInclude(message => message.Upit)
                    .ThenInclude(ticket => ticket.Korisnik)
            .Where(entry => entry.PoslanoUtc == null &&
                            entry.BrojPokusaja < 5 &&
                            entry.SljedeciPokusajUtc <= now)
            .OrderBy(entry => entry.KreiranoUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var entry in messages)
        {
            try
            {
                var user = entry.Poruka.Upit.Korisnik;
                await emailSender.SendSupportReplyEmailAsync(
                    entry.Primalac,
                    $"{user.Ime} {user.Prezime}".Trim(),
                    entry.Poruka.Upit.Naslov,
                    entry.Poruka.Sadrzaj,
                    entry.DetaljiUrl,
                    entry.Jezik);
                entry.PoslanoUtc = DateTime.UtcNow;
                entry.ZadnjaGreska = null;
            }
            catch (Exception exception)
            {
                entry.BrojPokusaja++;
                entry.ZadnjaGreska = exception.Message.Length > 2000
                    ? exception.Message[..2000]
                    : exception.Message;
                entry.SljedeciPokusajUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, entry.BrojPokusaja));
                _logger.LogWarning(exception, "Support email attempt {Attempt} failed for outbox item {OutboxId}.", entry.BrojPokusaja, entry.EmailPodrskeOutboxID);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
