using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class UserAchievement
{
    public string UserId { get; set; } = null!;

    public string AchievementId { get; set; } = null!;

    public DateOnly ReceivedDate { get; set; }

    public string? EnrollmentId { get; set; }

    public virtual Achievement Achievement { get; set; } = null!;

    public virtual Enrollment? Enrollment { get; set; }

    public virtual Account User { get; set; } = null!;
}
