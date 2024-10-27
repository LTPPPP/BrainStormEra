using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class LessonCompletion
{
    public string CompletionId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? LessonId { get; set; }

    public DateTime CompletionDate { get; set; }

    public virtual Lesson? Lesson { get; set; }

    public virtual Account? User { get; set; }
}
