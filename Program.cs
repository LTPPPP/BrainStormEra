﻿using BrainStormEra.Controllers;
using BrainStormEra.Models;
using BrainStormEra.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BrainStormEra.Repo;
using BrainStormEra.Repo.Chatbot;
using BrainStormEra.Repo.Admin;
namespace BrainStormEra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Đăng ký EmailService
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


            builder.Services.AddScoped<SwpMainContext>();
            builder.Services.AddScoped<AccountRepo>();
            builder.Services.AddScoped<AchievementRepo>();
            builder.Services.AddScoped<ChatbotRepo>();
            builder.Services.AddScoped<ProfileRepo>();
            builder.Services.AddScoped<FeedbackRepo>();
            builder.Services.AddScoped<LessonRepo>();

            builder.Services.AddControllersWithViews();
            // Add authentication services for cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/LoginPage";  // Redirect to login page if unauthorized
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Set cookie expiration time
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true; // Secure the cookie
                    options.Cookie.IsEssential = true;  // Ensure it's essential for GDPR compliance
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Enable HTTPS redirection and static file serving
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.MapControllers();

            app.UseRouting();
            // Enable cookie authentication
            app.UseAuthentication();

            // Enable authorization middleware
            app.UseAuthorization();

            // Map the controller routes with default route settings
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}");

            app.Run();
        }
    }
}