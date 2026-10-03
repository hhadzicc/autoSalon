using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Autosalon_OneZone.Models.ViewModels;

public sealed class SupportTicketListViewModel
{
    public IReadOnlyList<SupportTicketListItemViewModel> Tickets { get; init; } = Array.Empty<SupportTicketListItemViewModel>();
}

public sealed class SupportTicketListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusUpita Status { get; init; }
    public DateTime LastActivityUtc { get; init; }
    public int UnreadCount { get; init; }
}

public sealed class SupportConversationViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusUpita Status { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime LastActivityUtc { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? AssignedAgentId { get; init; }
    public string? AssignedAgentName { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<SupportMessageViewModel> Messages { get; init; } = Array.Empty<SupportMessageViewModel>();
}

public sealed class SupportMessageViewModel
{
    public int Id { get; init; }
    public string SenderName { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TipAutoraPorukePodrske SenderType { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime SentUtc { get; init; }
}

public class SupportMessageInputViewModel
{
    [Required(ErrorMessage = "Validation.MessageRequired")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Validation.SupportMessageLength")]
    public string Message { get; set; } = string.Empty;
}

public sealed class SupportStaffReplyViewModel : SupportMessageInputViewModel
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public enum SupportOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidState,
    Conflict
}

public sealed record SupportOperationResult(
    SupportOperationStatus Status,
    SupportConversationViewModel? Conversation = null,
    string? AssignedAgentName = null);
