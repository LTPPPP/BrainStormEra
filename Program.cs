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
using BrainStormEra.Repo.Certificate;

namespace BrainStormEra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Register Services
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

            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<OtpService>();
            builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();


            // Register Repositories
            builder.Services.AddScoped<AccountRepo>();
            builder.Services.AddScoped<AchievementRepo>();
            builder.Services.AddScoped<AdminRepo>();
            builder.Services.AddScoped<ChatbotRepo>();
            builder.Services.AddScoped<ProfileRepo>();
            builder.Services.AddScoped<FeedbackRepo>();
            builder.Services.AddScoped<LessonRepo>();
            builder.Services.AddScoped<CourseRepo>();
            builder.Services.AddScoped<ChapterRepo>();

            builder.Services.AddControllersWithViews();

            // Cookie Authentication Configuration
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

            // Configure Middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Controller Routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}");

            app.Run();
        }
    }
}
