using Microsoft.AspNetCore.Authentication.Cookies;

namespace HarmonySound.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
