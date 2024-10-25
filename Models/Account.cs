using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Account
{
    public string UserId { get; set; } = null!;

    public int? UserRole { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string? FullName { get; set; }

    public decimal? PaymentPoint { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public string? UserAddress { get; set; }

    public string? UserPicture { get; set; }

    public DateTime AccountCreatedAt { get; set; }

    public virtual ICollection<ChatbotConversation> ChatbotConversations { get; set; } = new List<ChatbotConversation>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual Role? UserRoleNavigation { get; set; }
}
