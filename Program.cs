using BrainStormEra.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure DbContext
            builder.Services.AddDbContext<SwpDb7Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SwpDb7Context")));

            // Add authentication service
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/LoginPage";  // Login page path
                    options.LogoutPath = "/Login/Logout";    // Logout path
                    options.AccessDeniedPath = "/Home/HomePageAdmin";  // Access denied page
                    options.SlidingExpiration = true;  // Sliding expiration for session
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);  // Session expiry time
                });

            // Add session handling with configuration
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(3600);  // Session timeout
                options.Cookie.HttpOnly = true;  // Cookie protection from client-side scripts
                options.Cookie.IsEssential = true;  // Mark cookie as essential
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable session handling before authentication and authorization
            app.UseSession();

            // Enable authentication and authorization middlewares
            app.UseAuthentication();
            app.UseAuthorization();

            // Map the controller routes
            app.MapControllerRoute(
                name: "home",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}");

            app.Run();
        }
    }
}
