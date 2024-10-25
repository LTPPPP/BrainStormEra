using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Feedback
{
    public string FeedbackId { get; set; } = null!;

    public string? CourseId { get; set; }

    public string? UserId { get; set; }

    public byte? StarRating { get; set; }

    public string? Comment { get; set; }

    public DateOnly FeedbackDate { get; set; }

    public bool? HiddenStatus { get; set; }

    public DateTime FeedbackCreatedAt { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Account? User { get; set; }
}
