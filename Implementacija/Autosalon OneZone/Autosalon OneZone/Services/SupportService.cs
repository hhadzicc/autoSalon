using Autosalon_OneZone.Data;
using Autosalon_OneZone.Logging;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Autosalon_OneZone.Services;

public interface ISupportService
{
    Task<int> CreateTicketAsync(string userId, string title, string content, string culture);
    Task<SupportTicketListViewModel> GetUserTicketsAsync(string userId);
    Task<SupportConversationViewModel?> GetUserConversationAsync(int id, string userId, bool markRead = true);
    Task<SupportOperationResult> SendUserMessageAsync(int id, string userId, string content);
    Task<SupportOperationResult> ReopenAsync(int id, string userId);
    Task<int> GetUnreadUserCountAsync(string userId);
    Task<SupportConversationViewModel?> GetStaffConversationAsync(
        int id,
        string agentId,
        bool isAdministrator,
        bool markRead = true);
    Task<SupportOperationResult> TakeAsync(int id, string agentId, string rowVersion, bool allowTakeover);
    Task<SupportOperationResult> ReleaseAsync(int id, string agentId, string rowVersion, bool allowTakeover);
    Task<SupportOperationResult> ReplyAsync(int id, string agentId, SupportStaffReplyViewModel input, string detailsUrl);
    Task<SupportOperationResult> CloseAsync(int id, string agentId, string rowVersion, bool allowTakeover);
}

public sealed class SupportService : ISupportService
{
    private readonly ApplicationDbContext _context;
    private readonly DemoOptions _demoOptions;
    private readonly ResendEmailOptions _emailOptions;
    private readonly IAuditLogger _audit;

    public SupportService(
        ApplicationDbContext context,
        IOptions<DemoOptions> demoOptions,
        IOptions<ResendEmailOptions> emailOptions,
        IAuditLogger? audit = null)
    {
        _context = context;
        _demoOptions = demoOptions.Value;
        _emailOptions = emailOptions.Value;
        _audit = audit ?? NullAuditLogger.Instance;
    }

    public async Task<int> CreateTicketAsync(string userId, string title, string content, string culture)
    {
        var now = DateTime.UtcNow;
        var ticket = new Podrska
        {
            KorisnikId = userId,
            Naslov = title.Trim(),
            DatumUpita = now,
            DatumZadnjeAktivnosti = now,
            Status = StatusUpita.CekaPodrsku,
            Jezik = NormalizeCulture(culture),
            Poruke = new List<PorukaPodrske>
            {
                new()
                {
                    PosiljalacId = userId,
                    TipAutora = TipAutoraPorukePodrske.Korisnik,
                    Sadrzaj = content.Trim(),
                    DatumSlanja = now
                }
            }
        };

        _context.PodrskaUpiti.Add(ticket);
        await _context.SaveChangesAsync();
        _audit.Success(
            "SupportTicketCreated",
            "SupportTicket",
            ticket.UpitID.ToString(),
            new
            {
                ticket.Naslov,
                Message = ticket.Poruke.First().Sadrzaj,
                ticket.Jezik,
                ticket.KorisnikId
            });
        return ticket.UpitID;
    }

    public async Task<SupportTicketListViewModel> GetUserTicketsAsync(string userId)
    {
        var tickets = await _context.PodrskaUpiti
            .AsNoTracking()
            .Where(ticket => ticket.KorisnikId == userId)
            .OrderByDescending(ticket => ticket.DatumZadnjeAktivnosti)
            .Select(ticket => new SupportTicketListItemViewModel
            {
                Id = ticket.UpitID,
                Title = ticket.Naslov,
                Status = ticket.Status,
                LastActivityUtc = ticket.DatumZadnjeAktivnosti,
                UnreadCount = ticket.Poruke.Count(message =>
                    message.TipAutora != TipAutoraPorukePodrske.Korisnik &&
                    message.ProcitanaUtc == null)
            })
            .ToListAsync();

        return new SupportTicketListViewModel { Tickets = tickets };
    }

    public async Task<SupportConversationViewModel?> GetUserConversationAsync(int id, string userId, bool markRead = true)
    {
        var ticket = await ConversationQuery()
            .FirstOrDefaultAsync(entry => entry.UpitID == id && entry.KorisnikId == userId);
        if (ticket == null)
        {
            return null;
        }

        if (markRead)
        {
            var now = DateTime.UtcNow;
            foreach (var message in ticket.Poruke.Where(message =>
                         message.TipAutora != TipAutoraPorukePodrske.Korisnik &&
                         message.ProcitanaUtc == null))
            {
                message.ProcitanaUtc = now;
            }
            await _context.SaveChangesAsync();
        }

        return MapConversation(ticket);
    }

    public async Task<SupportOperationResult> SendUserMessageAsync(int id, string userId, string content)
    {
        var ticket = await ConversationQuery()
            .FirstOrDefaultAsync(entry => entry.UpitID == id && entry.KorisnikId == userId);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.Status == StatusUpita.Zatvoren)
        {
            return new(SupportOperationStatus.InvalidState);
        }

        var now = DateTime.UtcNow;
        ticket.Poruke.Add(new PorukaPodrske
        {
            PosiljalacId = userId,
            TipAutora = TipAutoraPorukePodrske.Korisnik,
            Sadrzaj = content.Trim(),
            DatumSlanja = now
        });
        ticket.Status = ticket.DodijeljenKorisnikId == null
            ? StatusUpita.CekaPodrsku
            : StatusUpita.UObradi;
        ticket.DatumZadnjeAktivnosti = now;
        ticket.DatumRjesavanja = null;
        ticket.DatumZatvaranja = null;
        ticket.DatumPodsjetnika = null;
        await _context.SaveChangesAsync();
        _audit.Success(
            "SupportUserMessageSent",
            "SupportTicket",
            ticket.UpitID.ToString(),
            new
            {
                ticket.Naslov,
                Message = content.Trim(),
                SenderUserId = userId,
                Status = ticket.Status.ToString()
            });
        return new(SupportOperationStatus.Success, MapConversation(ticket));
    }

    public async Task<SupportOperationResult> ReopenAsync(int id, string userId)
    {
        var ticket = await ConversationQuery()
            .FirstOrDefaultAsync(entry => entry.UpitID == id && entry.KorisnikId == userId);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.Status != StatusUpita.Zatvoren)
        {
            return new(SupportOperationStatus.InvalidState, MapConversation(ticket));
        }

        var now = DateTime.UtcNow;
        ticket.Status = ticket.DodijeljenKorisnikId == null
            ? StatusUpita.CekaPodrsku
            : StatusUpita.UObradi;
        ticket.DatumZatvaranja = null;
        ticket.DatumRjesavanja = null;
        ticket.DatumPodsjetnika = null;
        ticket.DatumZadnjeAktivnosti = now;
        ticket.Poruke.Add(SystemMessage(ticket.UpitID, "SupportSystemReopened", now));
        await _context.SaveChangesAsync();
        _audit.Success(
            "SupportTicketReopened",
            "SupportTicket",
            ticket.UpitID.ToString(),
            new { ticket.Naslov, UserId = userId, Status = ticket.Status.ToString() });
        return new(SupportOperationStatus.Success, MapConversation(ticket));
    }

    public Task<int> GetUnreadUserCountAsync(string userId) =>
        _context.PorukePodrske.CountAsync(message =>
            message.Upit.KorisnikId == userId &&
            message.TipAutora != TipAutoraPorukePodrske.Korisnik &&
            message.ProcitanaUtc == null);

    public async Task<SupportConversationViewModel?> GetStaffConversationAsync(
        int id,
        string agentId,
        bool isAdministrator,
        bool markRead = true)
    {
        var query = ConversationQuery().Where(entry => entry.UpitID == id);
        if (!isAdministrator)
        {
            query = query.Where(entry =>
                entry.DodijeljenKorisnikId == agentId ||
                (entry.DodijeljenKorisnikId == null && entry.Status != StatusUpita.Zatvoren));
        }

        var ticket = await query.FirstOrDefaultAsync();
        if (ticket == null)
        {
            return null;
        }

        if (markRead)
        {
            var now = DateTime.UtcNow;
            foreach (var message in ticket.Poruke.Where(message =>
                         message.TipAutora == TipAutoraPorukePodrske.Korisnik &&
                         message.ProcitanaUtc == null))
            {
                message.ProcitanaUtc = now;
            }
            await _context.SaveChangesAsync();
        }

        return MapConversation(ticket);
    }

    public async Task<SupportOperationResult> TakeAsync(int id, string agentId, string rowVersion, bool allowTakeover)
    {
        var ticket = await ConversationQuery().FirstOrDefaultAsync(entry => entry.UpitID == id);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.Status == StatusUpita.Zatvoren)
        {
            return new(SupportOperationStatus.InvalidState);
        }
        if (ticket.DodijeljenKorisnikId != null && ticket.DodijeljenKorisnikId != agentId && !allowTakeover)
        {
            return new(SupportOperationStatus.Forbidden, AssignedAgentName: AgentName(ticket.DodijeljenKorisnik));
        }

        ApplyRowVersion(ticket, rowVersion);
        var agent = await _context.Users.FirstOrDefaultAsync(user => user.Id == agentId);
        if (agent == null)
        {
            return new(SupportOperationStatus.NotFound);
        }

        var now = DateTime.UtcNow;
        ticket.DodijeljenKorisnikId = agentId;
        ticket.DodijeljenKorisnik = agent;
        ticket.DatumDodjele = now;
        ticket.Status = StatusUpita.UObradi;
        ticket.DatumZadnjeAktivnosti = now;
        var result = await SaveWithConcurrencyAsync(ticket);
        if (result.Status == SupportOperationStatus.Success)
        {
            _audit.Success(
                "SupportTicketAssigned",
                "SupportTicket",
                ticket.UpitID.ToString(),
                new
                {
                    ticket.Naslov,
                    AgentUserId = agentId,
                    AgentName = AgentName(agent),
                    Takeover = allowTakeover
                });
        }
        return result;
    }

    public async Task<SupportOperationResult> ReleaseAsync(int id, string agentId, string rowVersion, bool allowTakeover)
    {
        var ticket = await ConversationQuery().FirstOrDefaultAsync(entry => entry.UpitID == id);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.DodijeljenKorisnikId != agentId && !allowTakeover)
        {
            return new(SupportOperationStatus.Forbidden, AssignedAgentName: AgentName(ticket.DodijeljenKorisnik));
        }

        ApplyRowVersion(ticket, rowVersion);
        ticket.DodijeljenKorisnikId = null;
        ticket.DodijeljenKorisnik = null;
        ticket.DatumDodjele = null;
        if (ticket.Status != StatusUpita.Zatvoren)
        {
            ticket.Status = StatusUpita.CekaPodrsku;
        }
        ticket.DatumZadnjeAktivnosti = DateTime.UtcNow;
        var result = await SaveWithConcurrencyAsync(ticket);
        if (result.Status == SupportOperationStatus.Success)
        {
            _audit.Success(
                "SupportTicketReleased",
                "SupportTicket",
                ticket.UpitID.ToString(),
                new { ticket.Naslov, AgentUserId = agentId, Takeover = allowTakeover });
        }
        return result;
    }

    public async Task<SupportOperationResult> ReplyAsync(int id, string agentId, SupportStaffReplyViewModel input, string detailsUrl)
    {
        var ticket = await ConversationQuery().FirstOrDefaultAsync(entry => entry.UpitID == id);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.DodijeljenKorisnikId != agentId)
        {
            return new(SupportOperationStatus.Forbidden, AssignedAgentName: AgentName(ticket.DodijeljenKorisnik));
        }
        if (ticket.Status == StatusUpita.Zatvoren)
        {
            return new(SupportOperationStatus.InvalidState, MapConversation(ticket));
        }

        ApplyRowVersion(ticket, input.RowVersion);
        var now = DateTime.UtcNow;
        var message = new PorukaPodrske
        {
            PosiljalacId = agentId,
            TipAutora = TipAutoraPorukePodrske.Osoblje,
            Sadrzaj = input.Message.Trim(),
            DatumSlanja = now
        };
        ticket.Poruke.Add(message);
        ticket.Status = StatusUpita.CekaKorisnika;
        ticket.DatumRjesavanja = null;
        ticket.DatumPodsjetnika = null;
        ticket.DatumZadnjeAktivnosti = now;

        if (!_demoOptions.Enabled && _emailOptions.Enabled && !string.IsNullOrWhiteSpace(ticket.Korisnik.Email))
        {
            message.EmailOutbox = new EmailPodrskeOutbox
            {
                Primalac = ticket.Korisnik.Email,
                Jezik = ticket.Jezik,
                KreiranoUtc = now,
                SljedeciPokusajUtc = now,
                DetaljiUrl = detailsUrl
            };
        }

        var result = await SaveWithConcurrencyAsync(ticket);
        if (result.Status == SupportOperationStatus.Success)
        {
            _audit.Success(
                "SupportStaffReplySent",
                "SupportTicket",
                ticket.UpitID.ToString(),
                new
                {
                    ticket.Naslov,
                    Message = input.Message.Trim(),
                    AgentUserId = agentId,
                    CustomerUserId = ticket.KorisnikId,
                    CustomerEmail = ticket.Korisnik.Email,
                    Status = ticket.Status.ToString()
                });
        }
        return result;
    }

    public async Task<SupportOperationResult> CloseAsync(int id, string agentId, string rowVersion, bool allowTakeover)
    {
        var ticket = await ConversationQuery().FirstOrDefaultAsync(entry => entry.UpitID == id);
        if (ticket == null)
        {
            return new(SupportOperationStatus.NotFound);
        }
        if (ticket.DodijeljenKorisnikId != agentId && !allowTakeover)
        {
            return new(SupportOperationStatus.Forbidden, AssignedAgentName: AgentName(ticket.DodijeljenKorisnik));
        }

        ApplyRowVersion(ticket, rowVersion);
        var now = DateTime.UtcNow;
        ticket.Status = StatusUpita.Zatvoren;
        ticket.DatumZatvaranja = now;
        ticket.DatumZadnjeAktivnosti = now;
        ticket.Poruke.Add(SystemMessage(ticket.UpitID, "SupportSystemClosed", now));
        var result = await SaveWithConcurrencyAsync(ticket);
        if (result.Status == SupportOperationStatus.Success)
        {
            _audit.Success(
                "SupportTicketClosed",
                "SupportTicket",
                ticket.UpitID.ToString(),
                new { ticket.Naslov, AgentUserId = agentId, Takeover = allowTakeover });
        }
        return result;
    }

    private IQueryable<Podrska> ConversationQuery() =>
        _context.PodrskaUpiti
            .Include(ticket => ticket.Korisnik)
            .Include(ticket => ticket.DodijeljenKorisnik)
            .Include(ticket => ticket.Poruke)
                .ThenInclude(message => message.Posiljalac);

    private async Task<SupportOperationResult> SaveWithConcurrencyAsync(Podrska ticket)
    {
        try
        {
            await _context.SaveChangesAsync();
            return new(SupportOperationStatus.Success, MapConversation(ticket));
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(SupportOperationStatus.Conflict);
        }
    }

    private void ApplyRowVersion(Podrska ticket, string rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return;
        }

        try
        {
            _context.Entry(ticket).Property(entry => entry.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            _context.Entry(ticket).Property(entry => entry.RowVersion).OriginalValue = Array.Empty<byte>();
        }
    }

    private static PorukaPodrske SystemMessage(int ticketId, string content, DateTime now) => new()
    {
        UpitID = ticketId,
        TipAutora = TipAutoraPorukePodrske.Sistem,
        Sadrzaj = content,
        DatumSlanja = now
    };

    private static SupportConversationViewModel MapConversation(Podrska ticket) => new()
    {
        Id = ticket.UpitID,
        Title = ticket.Naslov,
        Status = ticket.Status,
        CreatedUtc = ticket.DatumUpita,
        LastActivityUtc = ticket.DatumZadnjeAktivnosti,
        CustomerName = AgentName(ticket.Korisnik),
        CustomerEmail = ticket.Korisnik?.Email ?? string.Empty,
        AssignedAgentId = ticket.DodijeljenKorisnikId,
        AssignedAgentName = AgentName(ticket.DodijeljenKorisnik),
        RowVersion = ticket.RowVersion.Length == 0 ? string.Empty : Convert.ToBase64String(ticket.RowVersion),
        Messages = ticket.Poruke
            .OrderBy(message => message.DatumSlanja)
            .ThenBy(message => message.PorukaPodrskeID)
            .Select(message => new SupportMessageViewModel
            {
                Id = message.PorukaPodrskeID,
                SenderName = message.TipAutora == TipAutoraPorukePodrske.Sistem
                    ? "Autosalon OneZone"
                    : AgentName(message.Posiljalac),
                SenderType = message.TipAutora,
                Content = message.Sadrzaj,
                SentUtc = message.DatumSlanja
            })
            .ToList()
    };

    private static string AgentName(ApplicationUser? user) => user == null
        ? string.Empty
        : $"{user.Ime} {user.Prezime}".Trim();

    private static string NormalizeCulture(string culture) =>
        culture.StartsWith("bs", StringComparison.OrdinalIgnoreCase) ? "bs-Latn-BA" : "en-US";
}
