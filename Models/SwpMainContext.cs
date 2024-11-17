using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra.Models;

public partial class SwpMainContext : DbContext
{
    public SwpMainContext()
    {
    }

    public SwpMainContext(DbContextOptions<SwpMainContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<ChatbotConversation> ChatbotConversations { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseCategory> CourseCategories { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonCompletion> LessonCompletions { get; set; }

    public virtual DbSet<LessonType> LessonTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<UserAchievement> UserAchievements { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server= USER\\SQLSERVER13;Database=SWP_MAIN;uid=sa;pwd=password@;encrypt=true;trustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__account__B9BE370F903B6661");

            entity.ToTable("account");

            entity.HasIndex(e => e.UserEmail, "UQ__account__B0FBA212C47BE922").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__account__F3DBC5720708EA48").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.AccountCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("account_created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.PaymentPoint)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("payment_point");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.UserAddress).HasColumnName("user_address");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_email");
            entity.Property(e => e.UserPicture).HasColumnName("user_picture");
            entity.Property(e => e.UserRole).HasColumnName("user_role");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasOne(d => d.UserRoleNavigation).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.UserRole)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__account__user_ro__7A672E12");
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.AchievementId).HasName("PK__achievem__3C492E8329F77E2F");

            entity.ToTable("achievement");

            entity.Property(e => e.AchievementId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("achievement_id");
            entity.Property(e => e.AchievementCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("achievement_created_at");
            entity.Property(e => e.AchievementDescription).HasColumnName("achievement_description");
            entity.Property(e => e.AchievementIcon)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("achievement_icon");
            entity.Property(e => e.AchievementName)
                .HasMaxLength(255)
                .HasColumnName("achievement_name");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__chapter__745EFE87AEB9D82F");

            entity.ToTable("chapter");

            entity.HasIndex(e => new { e.CourseId, e.ChapterOrder }, "unique_chapter_order_per_course").IsUnique();

            entity.Property(e => e.ChapterId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("chapter_id");
            entity.Property(e => e.ChapterCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("chapter_created_at");
            entity.Property(e => e.ChapterDescription).HasColumnName("chapter_description");
            entity.Property(e => e.ChapterName)
                .HasMaxLength(255)
                .HasColumnName("chapter_name");
            entity.Property(e => e.ChapterOrder).HasColumnName("chapter_order");
            entity.Property(e => e.ChapterStatus).HasColumnName("chapter_status");
            entity.Property(e => e.CourseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_id");

            entity.HasOne(d => d.ChapterStatusNavigation).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.ChapterStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__chapter__chapter__7B5B524B");

            entity.HasOne(d => d.Course).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__chapter__course___7C4F7684");
        });

        modelBuilder.Entity<ChatbotConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__chatbot___311E7E9AC59FF60F");

            entity.ToTable("chatbot_conversation");

            entity.Property(e => e.ConversationId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("conversation_id");
            entity.Property(e => e.ConversationContent).HasColumnName("conversation_content");
            entity.Property(e => e.ConversationTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("conversation_time");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ChatbotConversations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__chatbot_c__user___7D439ABD");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__course__8F1EF7AE575BE3AA");

            entity.ToTable("course");

            entity.Property(e => e.CourseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CourseCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("course_created_at");
            entity.Property(e => e.CourseDescription).HasColumnName("course_description");
            entity.Property(e => e.CourseName)
                .HasMaxLength(255)
                .HasColumnName("course_name");
            entity.Property(e => e.CoursePicture)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_picture");
            entity.Property(e => e.CourseStatus).HasColumnName("course_status");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            entity.HasOne(d => d.CourseStatusNavigation).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CourseStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__course__course_s__7E37BEF6");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__course__created___7F2BE32F");

            entity.HasMany(d => d.CourseCategories).WithMany(p => p.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseCategoryMapping",
                    r => r.HasOne<CourseCategory>().WithMany()
                        .HasForeignKey("CourseCategoryId")
                        .HasConstraintName("FK__course_ca__cours__01142BA1"),
                    l => l.HasOne<Course>().WithMany()
                        .HasForeignKey("CourseId")
                        .HasConstraintName("FK__course_ca__cours__00200768"),
                    j =>
                    {
                        j.HasKey("CourseId", "CourseCategoryId").HasName("PK__course_c__10F9222051440CEB");
                        j.ToTable("course_category_mapping");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(255)
                            .IsUnicode(false)
                            .HasColumnName("course_id");
                        j.IndexerProperty<string>("CourseCategoryId")
                            .HasMaxLength(255)
                            .IsUnicode(false)
                            .HasColumnName("course_category_id");
                    });
        });

        modelBuilder.Entity<CourseCategory>(entity =>
        {
            entity.HasKey(e => e.CourseCategoryId).HasName("PK__course_c__FE7D58E89BCB7AFA");

            entity.ToTable("course_category");

            entity.Property(e => e.CourseCategoryId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_category_id");
            entity.Property(e => e.CourseCategoryName)
                .HasMaxLength(255)
                .HasColumnName("course_category_name");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__enrollme__6D24AA7A304DBA63");

            entity.ToTable("enrollment");

            entity.Property(e => e.EnrollmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("enrollment_id");
            entity.Property(e => e.Approved)
                .HasDefaultValue(false)
                .HasColumnName("approved");
            entity.Property(e => e.CertificateIssuedDate).HasColumnName("certificate_issued_date");
            entity.Property(e => e.CourseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.EnrollmentCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("enrollment_created_at");
            entity.Property(e => e.EnrollmentStatus).HasColumnName("enrollment_status");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__enrollmen__cours__02084FDA");

            entity.HasOne(d => d.EnrollmentStatusNavigation).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.EnrollmentStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__enrollmen__enrol__02FC7413");

            entity.HasOne(d => d.User).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__enrollmen__user___03F0984C");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__feedback__7A6B2B8CF4DA3A23");

            entity.ToTable("feedback");

            entity.Property(e => e.FeedbackId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("feedback_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CourseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.FeedbackCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("feedback_created_at");
            entity.Property(e => e.FeedbackDate).HasColumnName("feedback_date");
            entity.Property(e => e.HiddenStatus)
                .HasDefaultValue(false)
                .HasColumnName("hidden_status");
            entity.Property(e => e.StarRating).HasColumnName("star_rating");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__feedback__course__04E4BC85");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__feedback__user_i__05D8E0BE");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__lesson__6421F7BE3D64F286");

            entity.ToTable("lesson");

            entity.HasIndex(e => new { e.ChapterId, e.LessonOrder }, "unique_lesson_order_per_chapter").IsUnique();

            entity.Property(e => e.LessonId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.ChapterId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("chapter_id");
            entity.Property(e => e.LessonContent).HasColumnName("lesson_content");
            entity.Property(e => e.LessonCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("lesson_created_at");
            entity.Property(e => e.LessonDescription).HasColumnName("lesson_description");
            entity.Property(e => e.LessonName)
                .HasMaxLength(255)
                .HasColumnName("lesson_name");
            entity.Property(e => e.LessonOrder).HasColumnName("lesson_order");
            entity.Property(e => e.LessonStatus).HasColumnName("lesson_status");
            entity.Property(e => e.LessonTypeId).HasColumnName("lesson_type_id");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__lesson__chapter___06CD04F7");

            entity.HasOne(d => d.LessonStatusNavigation).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonStatus)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_s__07C12930");

            entity.HasOne(d => d.LessonType).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.LessonTypeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__lesson__lesson_t__08B54D69");
        });

        modelBuilder.Entity<LessonCompletion>(entity =>
        {
            entity.HasKey(e => e.CompletionId).HasName("PK__lesson_c__FE426E98B8A2965A");

            entity.ToTable("lesson_completion");

            entity.Property(e => e.CompletionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("completion_id");
            entity.Property(e => e.CompletionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("completion_date");
            entity.Property(e => e.LessonId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("lesson_id");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Lesson).WithMany(p => p.LessonCompletions)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__lesson_co__lesso__09A971A2");

            entity.HasOne(d => d.User).WithMany(p => p.LessonCompletions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__lesson_co__user___0A9D95DB");
        });

        modelBuilder.Entity<LessonType>(entity =>
        {
            entity.HasKey(e => e.LessonTypeId).HasName("PK__lesson_t__F5960D1ED1F478DA");

            entity.ToTable("lesson_type");

            entity.Property(e => e.LessonTypeId)
                .ValueGeneratedNever()
                .HasColumnName("lesson_type_id");
            entity.Property(e => e.LessonTypeName)
                .HasMaxLength(255)
                .HasColumnName("lesson_type_name");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__notifica__E059842FFC4F43C6");

            entity.ToTable("notification");

            entity.Property(e => e.NotificationId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("notification_id");
            entity.Property(e => e.CourseId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("course_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.NotificationContent).HasColumnName("notification_content");
            entity.Property(e => e.NotificationCreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("notification_created_at");
            entity.Property(e => e.NotificationTitle)
                .HasMaxLength(255)
                .HasColumnName("notification_title");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("notification_type");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__notificat__cours__0B91BA14");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__notificat__creat__0C85DE4D");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__notificat__user___0D7A0286");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__payment__ED1FC9EA1616F53F");

            entity.ToTable("payment");

            entity.Property(e => e.PaymentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentDescription)
                .HasMaxLength(255)
                .HasColumnName("payment_description");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_status");
            entity.Property(e => e.PointsEarned).HasColumnName("points_earned");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__payment__user_id__0E6E26BF");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.UserRole).HasName("PK__role__68057FEC035AE1AB");

            entity.ToTable("role");

            entity.Property(e => e.UserRole)
                .ValueGeneratedNever()
                .HasColumnName("user_role");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__status__3683B5314F02216C");

            entity.ToTable("status");

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("status_id");
            entity.Property(e => e.StatusDescription)
                .HasMaxLength(100)
                .HasColumnName("status_description");
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AchievementId }).HasName("PK__user_ach__9A7AA5E75DB95843");

            entity.ToTable("user_achievement");

            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_id");
            entity.Property(e => e.AchievementId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("achievement_id");
            entity.Property(e => e.EnrollmentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("enrollment_id");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date");

            entity.HasOne(d => d.Achievement).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.AchievementId)
                .HasConstraintName("FK__user_achi__achie__0F624AF8");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.EnrollmentId)
                .HasConstraintName("FK__user_achi__enrol__10566F31");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__user_achi__user___114A936A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
