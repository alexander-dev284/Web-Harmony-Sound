namespace HarmonySound.MVC
{
    // URL base de la API. Se establece en Program.cs desde la variable de entorno
    // "ApiUrl" (en producción) o cae a localhost para desarrollo local.
    public static class ApiConfig
    {
        public static string BaseUrl { get; set; } = "https://localhost:7120";
    }
}
