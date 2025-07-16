using HarmonySound.Models;
using HarmonySound.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using HarmonySound.API.Services;
using Microsoft.AspNetCore.Identity.UI.Services; 

namespace HarmonySound.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<HarmonySoundDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("HarmonySoundDbContext") ?? throw new InvalidOperationException("Connection string 'HarmonySoundDbContext' not found.")));

            // Add services to the container.
            builder.Services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<HarmonySoundDbContext>()
                .AddDefaultTokenProviders();

            // Configurar la autenticación JWT
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };
                });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100 MB
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Registro de servicios personalizados
            builder.Services.AddTransient<IEmailSender, EmailService>();
            builder.Services.AddTransient<IJwtService, JwtService>();
            builder.Services.AddTransient<I2FAService, TwoFactorAuthService>();
            builder.Services.AddTransient<IPayPalService, PayPalService>();
            builder.Services.AddMemoryCache(); // Necesario para TwoFactorAuthService

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            
            // AGREGAR ESTA LÍNEA PARA SERVIR ARCHIVOS ESTÁTICOS
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
