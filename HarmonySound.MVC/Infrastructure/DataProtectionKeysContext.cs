using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.MVC.Infrastructure
{
    // Contexto mínimo usado únicamente para persistir las claves de Data Protection en PostgreSQL.
    // Sin esto, las claves viven en el sistema de archivos del contenedor y Render las descarta en
    // cada redeploy, invalidando las cookies de sesión/login y los tokens antiforgery existentes.
    public class DataProtectionKeysContext : DbContext, IDataProtectionKeyContext
    {
        public DataProtectionKeysContext(DbContextOptions<DataProtectionKeysContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    }
}
