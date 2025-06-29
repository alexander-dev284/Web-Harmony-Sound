using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HarmonySound.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Crud<Album>.EndPoint = "https://localhost:7120/api/Albums";
            Crud<ContentAlbum>.EndPoint = "https://localhost:7120/api/ContentAlbums";
            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            Crud<Plan>.EndPoint = "https://localhost:7120/api/Plans";
            Crud<Report>.EndPoint = "https://localhost:7120/api/Reports";
            Crud<Role>.EndPoint = "https://localhost:7120/api/Roles";
            Crud<Statistic>.EndPoint = "https://localhost:7120/api/Statistics";
            Crud<SubscriptionHistory>.EndPoint = "https://localhost:7120/api/SubscriptionsHistories";
            Crud<UserRole>.EndPoint = "https://localhost:7120/api/UserRoles";
            Crud<User>.EndPoint = "https://localhost:7120/api/Users";
            Crud<UserPlan>.EndPoint = "https://localhost:7120/api/UsersPlans";



            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDistributedMemoryCache();  // Usar en memoria como almacenamiento para las sesiones
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de espera para la sesión
                options.Cookie.HttpOnly = true;  // Hace que la cookie no sea accesible por JavaScript
                options.Cookie.IsEssential = true;  // Hace la cookie esencial para el funcionamiento
            });

            // Configuración de autenticación por cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                });

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            // Agrega estos middlewares en este orden
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
