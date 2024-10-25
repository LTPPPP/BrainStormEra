using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Achievement
{
    public string AchievementId { get; set; } = null!;

    public string AchievementName { get; set; } = null!;

    public string? AchievementDescription { get; set; }

    public string? AchievementIcon { get; set; }

    public DateTime AchievementCreatedAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
