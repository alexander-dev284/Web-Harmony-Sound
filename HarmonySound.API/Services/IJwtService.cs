using HarmonySound.Models;
using System.Security.Claims;

namespace HarmonySound.API.Services
{
    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(User user);
        ClaimsPrincipal? ValidateToken(string token);
        string GenerateRefreshToken();
    }
}
