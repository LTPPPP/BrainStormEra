using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class ChatbotConversation
{
    public string ConversationId { get; set; } = null!;

    public string? UserId { get; set; }

    public DateTime ConversationTime { get; set; }

    public string ConversationContent { get; set; } = null!;

    public virtual Account? User { get; set; }
}
