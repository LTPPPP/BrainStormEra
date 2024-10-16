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

            builder.Services.AddDbContext<SwpDb7Context>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add authentication service
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Home/LoginPage";  // Set the login page path
                    options.LogoutPath = "/Login/Logout";  // Set the logout path
                    options.AccessDeniedPath = "/Home/HomePageAdmin";  // Optional: set a page for access denied
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = 403;  // Return 403 Forbidden
                        return Task.CompletedTask;
                    };
                });

            // Add session handling
            builder.Services.AddSession();

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

            // Enable authentication and authorization middlewares
            app.UseAuthentication();  // Added for login functionality
            app.UseAuthorization();

            // Enable session handling
            app.UseSession();

            app.MapControllerRoute(
                name: "login",
                pattern: "{controller=Login}/{action=LoginPage}/{id?}"); 

            app.Run();
        }
    }
}
