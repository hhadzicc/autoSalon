using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Services;

public interface IAdminModerationService
{
    Task<bool> DeleteSupportRequestAsync(int id);
    Task<SupportStatusUpdateResult> UpdateSupportStatusAsync(int id, string status);
    Task<bool> DeleteReviewAsync(int id);
}

public sealed class AdminModerationService : IAdminModerationService
{
    private readonly ApplicationDbContext _context;

    public AdminModerationService(ApplicationDbContext context)
    {
        _context = context;
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
        return true;
    }

    public async Task<SupportStatusUpdateResult> UpdateSupportStatusAsync(int id, string status)
    {
        var request = await _context.PodrskaUpiti.FindAsync(id);
        if (request == null)
        {
            return SupportStatusUpdateResult.NotFound;
        }

        if (!Enum.TryParse<StatusUpita>(status, true, out var parsedStatus) ||
            !Enum.IsDefined(typeof(StatusUpita), parsedStatus))
        {
            return SupportStatusUpdateResult.InvalidStatus;
        }

        request.Status = parsedStatus;
        await _context.SaveChangesAsync();
        return SupportStatusUpdateResult.Updated;
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
        return true;
    }
}

public enum SupportStatusUpdateResult
{
    Updated,
    NotFound,
    InvalidStatus
}
