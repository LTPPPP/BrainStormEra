using BrainStormEra.Models;
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

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable session handling
            app.UseSession();

            app.UseAuthorization();

            // Map the controller routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}");

            app.Run();
        }
    }
}
