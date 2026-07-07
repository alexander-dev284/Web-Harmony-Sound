using Amazon.S3;
using Amazon.S3.Model;

namespace HarmonySound.API.Services
{
    // Almacenamiento de archivos en un servicio compatible con S3
    // (Backblaze B2, Cloudflare R2, Supabase, etc.). La configuración vive
    // en la sección "Storage" (idealmente en variables de entorno).
    public interface IStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder);
        Task DeleteByUrlAsync(string fileUrl);
    }

    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _publicBaseUrl;
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IConfiguration config, ILogger<S3StorageService> logger)
        {
            _logger = logger;
            var section = config.GetSection("Storage");
            _bucket = section["Bucket"] ?? string.Empty;
            _publicBaseUrl = (section["PublicBaseUrl"] ?? string.Empty).TrimEnd('/');

            var s3Config = new AmazonS3Config
            {
                ServiceURL = section["ServiceUrl"],
                ForcePathStyle = true,                 // necesario para endpoints S3-compatibles
                AuthenticationRegion = section["Region"]
            };
            _s3 = new AmazonS3Client(section["AccessKey"], section["SecretKey"], s3Config);
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder)
        {
            var key = string.IsNullOrEmpty(folder) ? fileName : $"{folder.Trim('/')}/{fileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                DisablePayloadSigning = true           // evita problemas de firma con B2/R2 al hacer streaming
            };

            await _s3.PutObjectAsync(request);
            return $"{_publicBaseUrl}/{key}";
        }

        public async Task DeleteByUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;

            var key = ExtractKey(fileUrl);
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el objeto {Key} del almacenamiento", key);
            }
        }

        // Obtiene el 'key' del objeto a partir de la URL pública guardada en la BD.
        private string ExtractKey(string fileUrl)
        {
            if (!string.IsNullOrEmpty(_publicBaseUrl) &&
                fileUrl.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return fileUrl.Substring(_publicBaseUrl.Length).TrimStart('/');
            }

            try
            {
                var path = new Uri(fileUrl).AbsolutePath.TrimStart('/');
                if (!string.IsNullOrEmpty(_bucket) &&
                    path.StartsWith(_bucket + "/", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(_bucket.Length + 1);
                }
                return path;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
