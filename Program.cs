using BrainStormEra.Controllers;
using BrainStormEra.Models;
using BrainStormEra.Services;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register GeminiApiService to be injected when needed
            builder.Services.AddHttpClient<GeminiApiService>();

            // Add services to the container, such as Controllers with Views
            builder.Services.AddControllersWithViews();

            // Configure DbContext with SQL Server
            builder.Services.AddDbContext<SwpDb7Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SwpDb7Context")));

            // Add session handling with configuration
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);  // Session timeout after 60 minutes of inactivity
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

            // Enable HTTPS redirection and static file serving
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable session handling
            app.UseSession();

            // Enable authorization middleware
            app.UseAuthorization();

            // Map the controller routes with default route settings
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=HomePageAdmin}/{id?}");

            app.Run();
        }
    }
}
