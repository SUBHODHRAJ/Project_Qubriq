using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Qubriq.API.Models;

public class WhatsAppConversation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string PhoneNumber { get; set; } = string.Empty; // Normalized E.164 without '+', e.g., 919876543210

    [MaxLength(128)]
    public string? Bsuid { get; set; } // Meta Business-Scoped User ID (user_id / from_user_id)

    [MaxLength(150)]
    public string? ProfileName { get; set; }

    public long? CustomerId { get; set; }
    public long? RecipientId { get; set; }

    [Required, MaxLength(32)]
    public string Status { get; set; } = "AI_Active"; // AI_Active | Escalated_To_Human | Closed

    public int? AssignedDispatcherId { get; set; }

    public DateTime? LastCustomerMessageAt { get; set; } // Tracks 24-hour customer service window
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();
}

public class WhatsAppMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }
    [ForeignKey(nameof(ConversationId))]
    public WhatsAppConversation Conversation { get; set; } = null!;

    [MaxLength(128)]
    public string? MetaMessageId { get; set; } // wamid - unique for deduplication & status tracking

    [Required, MaxLength(16)]
    public string Direction { get; set; } = "Inbound"; // Inbound | Outbound

    [Required, MaxLength(24)]
    public string SenderType { get; set; } = "Customer"; // Customer | AI | Dispatcher | System

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ToolInvoked { get; set; }

    [MaxLength(24)]
    public string DeliveryStatus { get; set; } = "Received"; // Received | Sent | Delivered | Read | Failed

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class WhatsAppOutbox
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string MessageType { get; set; } = "template"; // template | text

    [MaxLength(128)]
    public string? TemplateName { get; set; }

    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    [Required, MaxLength(24)]
    public string Status { get; set; } = "Pending"; // Pending | Sent | Failed

    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public class WhatsAppKnowledgeBase
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Category { get; set; } = "General";

    [Required, MaxLength(300)]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}