using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HarmonySound.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configuración de los endpoints de la API
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

            // Configuración de Serilog para registrar en la consola y en un archivo
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Imprime en la consola
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day) // Guarda los logs en un archivo
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // Configuración de los límites de tamaño de solicitud
            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100 MB
            });

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 104857600; // 100 MB
            });

            // Configuración de logging (se usa Serilog)
            builder.Logging.ClearProviders();  // Limpiar proveedores de logs predeterminados
            builder.Logging.AddSerilog();  // Usar Serilog para los logs

            // Configuración de autenticación por cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // Ruta para login
                    options.LogoutPath = "/Account/Logout"; // Ruta para logout
                });

            builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnSigningIn = context =>
                {
                    var identity = (System.Security.Claims.ClaimsIdentity)context.Principal.Identity;

                    // Mapea cualquier claim "role" o "roles" a ClaimTypes.Role
                    var roleClaims = identity.FindAll("role").ToList();
                    foreach (var rc in roleClaims)
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, rc.Value));
                    }
                    var rolesClaims = identity.FindAll("roles").ToList();
                    foreach (var rc in rolesClaims)
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, rc.Value));
                    }
                    // Mapea también el claim de rol con el namespace largo (el que realmente llega en tu JWT)
                    var msRoleClaims = identity.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
                    foreach (var rc in msRoleClaims)
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, rc.Value));
                    }
                    return Task.CompletedTask;
                };
            });

            // Configuración de sesiones en memoria (sin DbContext en MVC)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo máximo de inactividad
                options.Cookie.HttpOnly = true; // Hace que la cookie no sea accesible por JS
                options.Cookie.IsEssential = true; // Hace la cookie esencial
            });

            // Agregar controladores y vistas con opciones JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.MaxDepth = 64; // Opcional: aumenta la profundidad máxima si es necesario
                });
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession();  // Asegura que la sesión esté disponible

            // Inyectar HttpClient para hacer solicitudes a la API
            builder.Services.AddHttpClient(); // Esto permite usar HttpClient en tus controladores

            var app = builder.Build();

            // Configuración de la tubería HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error"); // Página de error para producción
                app.UseHsts();  // HSTS para seguridad adicional en producción
            }

            app.UseHttpsRedirection();  // Redirección a HTTPS
            app.UseStaticFiles();  // Permite archivos estáticos

            app.UseRouting();  // Usar el enrutamiento
            app.UseSession();  // Usar sesiones

            // Autenticación y autorización
            app.UseAuthentication();
            app.UseAuthorization();

            // Configurar la ruta de los controladores
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}"); // Ruta por defecto para Login

            // Global exception handling middleware
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        // Registra los errores no manejados en el archivo de logs
                        Log.Error(exceptionHandlerPathFeature.Error, "Unhandled exception occurred.");
                    }

                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An error occurred.");
                });
            });

            // Ejecutar la aplicación
            app.Run();
        }
    }
}
