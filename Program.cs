using BrainStormEra.Controllers;
using BrainStormEra.Models;
using BrainStormEra.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Chatbot;
using BrainStormEra.Repo.Admin;
using Microsoft.Extensions.Configuration;
using BrainStormEra.Repo.Chapter;
using BrainStormEra.Repo.Course;

namespace BrainStormEra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Đảm bảo tệp appsettings.json được tải
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Đăng ký EmailService và các dịch vụ HTTP và Session
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient<GeminiApiService>();

            builder.Services.AddSession();

            // Configure DbContext with SQL Server
            builder.Services.AddDbContext<SwpMainContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SwpMainContext")));

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<OtpService>();

            // Đăng ký các lớp Repo
            builder.Services.AddScoped<SwpMainContext>();
            builder.Services.AddScoped<AccountRepo>();
            builder.Services.AddScoped<AchievementRepo>();
            builder.Services.AddScoped<ChatbotRepo>();
            builder.Services.AddScoped<ProfileRepo>();
            builder.Services.AddScoped<FeedbackRepo>();
            builder.Services.AddScoped<LessonRepo>();
            builder.Services.AddScoped<CourseRepo>();
            builder.Services.AddScoped<ChapterRepo>();


            builder.Services.AddControllersWithViews();

            // Thêm dịch vụ xác thực bằng cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/LoginPage";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.MapControllers();

            app.UseRouting();

            // Enable authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controller routes with default route settings
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}");

            app.Run();
        }
    }
}
