using Autosalon_OneZone.Data;
using Autosalon_OneZone.Logging;
using Autosalon_OneZone.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Autosalon_OneZone.Services;

public sealed class SupportWorkflowOptions
{
    public int ReminderAfterDays { get; set; } = 5;
    public int CloseAnsweredAfterDays { get; set; } = 7;
}

public sealed class SupportLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SupportWorkflowOptions _options;
    private readonly ILogger<SupportLifecycleBackgroundService> _logger;

    public SupportLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SupportWorkflowOptions> options,
        ILogger<SupportLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Support ticket lifecycle processing failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        var now = DateTime.UtcNow;
        var answeredCloseBefore = now.AddDays(-_options.CloseAnsweredAfterDays);
        var reminderBefore = now.AddDays(-_options.ReminderAfterDays);

        var ticketsToClose = await context.PodrskaUpiti
            .Include(ticket => ticket.Poruke)
            .Where(ticket =>
                (ticket.Status == StatusUpita.CekaKorisnika || ticket.Status == StatusUpita.Rijesen) &&
                ticket.DatumZadnjeAktivnosti <= answeredCloseBefore)
            .ToListAsync(cancellationToken);

        foreach (var ticket in ticketsToClose)
        {
            ticket.Status = StatusUpita.Zatvoren;
            ticket.DatumZatvaranja = now;
            ticket.DatumZadnjeAktivnosti = now;
            ticket.Poruke.Add(SystemMessage(ticket.UpitID, "SupportSystemAutoClosed", now));
        }

        var ticketsToRemind = await context.PodrskaUpiti
            .Include(ticket => ticket.Poruke)
            .Where(ticket => ticket.Status == StatusUpita.CekaKorisnika &&
                             ticket.DatumPodsjetnika == null &&
                             ticket.DatumZadnjeAktivnosti <= reminderBefore &&
                             ticket.DatumZadnjeAktivnosti > answeredCloseBefore)
            .ToListAsync(cancellationToken);

        foreach (var ticket in ticketsToRemind)
        {
            ticket.DatumPodsjetnika = now;
            ticket.Poruke.Add(SystemMessage(ticket.UpitID, "SupportSystemReminder", now));
        }

        if (ticketsToClose.Count > 0 || ticketsToRemind.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Support lifecycle closed {ClosedCount} and reminded {ReminderCount} inquiries.",
                ticketsToClose.Count,
                ticketsToRemind.Count);

            foreach (var ticket in ticketsToClose)
            {
                audit.Success(
                    "SupportTicketAutoClosed",
                    "SupportTicket",
                    ticket.UpitID.ToString(),
                    new { ticket.Naslov, ticket.KorisnikId });
            }

            foreach (var ticket in ticketsToRemind)
            {
                audit.Success(
                    "SupportReminderCreated",
                    "SupportTicket",
                    ticket.UpitID.ToString(),
                    new { ticket.Naslov, ticket.KorisnikId });
            }
        }
    }

    private static PorukaPodrske SystemMessage(int ticketId, string content, DateTime now) => new()
    {
        UpitID = ticketId,
        TipAutora = TipAutoraPorukePodrske.Sistem,
        Sadrzaj = content,
        DatumSlanja = now
    };
}
