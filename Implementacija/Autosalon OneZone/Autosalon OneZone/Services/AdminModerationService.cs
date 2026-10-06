using Autosalon_OneZone.Data;
using Autosalon_OneZone.Logging;
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Services;

public interface IAdminModerationService
{
    Task<bool> DeleteSupportRequestAsync(int id);
    Task<bool> DeleteReviewAsync(int id);
}
public sealed class AdminModerationService : IAdminModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogger _audit;

    public AdminModerationService(ApplicationDbContext context, IAuditLogger? audit = null)
    {
        _context = context;
        _audit = audit ?? NullAuditLogger.Instance;
    }

    public async Task<bool> DeleteSupportRequestAsync(int id)
    {
        var request = await _context.PodrskaUpiti.FindAsync(id);
        if (request == null)
        {
            return false;
        }

        _context.PodrskaUpiti.Remove(request);
        await _context.SaveChangesAsync();
        _audit.Success(
            "SupportTicketDeleted",
            "SupportTicket",
            id.ToString(),
            new
            {
                request.Naslov,
                request.KorisnikId,
                Status = request.Status.ToString(),
                request.DatumUpita
            });
        return true;
    }

    public async Task<bool> DeleteReviewAsync(int id)
    {
        var review = await _context.Recenzije.FindAsync(id);
        if (review == null)
        {
            return false;
        }

        _context.Recenzije.Remove(review);
        await _context.SaveChangesAsync();
        _audit.Success(
            "ReviewDeletedByStaff",
            "Review",
            id.ToString(),
            new
            {
                review.KorisnikId,
                review.VoziloID,
                Rating = review.Ocjena,
                Comment = review.Komentar,
                review.DatumRecenzije
            });
        return true;
    }
}
