using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZEGU.WebApp.Services
{
    public class QrCodeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QrCodeService> _logger;

        public QrCodeService(IConfiguration configuration, ILogger<QrCodeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public byte[] GenerateQrCode(string content, int size = 300)
        {
            var baseUrl = _configuration["QrCodeApiUrl"] ?? "https://api.qrserver.com/v1/create-qr-code/";
            var url = $"{baseUrl}?size={size}x{size}&data={Uri.EscapeDataString(content)}";
            
            using var client = new HttpClient();
            return client.GetByteArrayAsync(url).GetAwaiter().GetResult();
        }

        public string GenerateAssetQrCode(int assetId, string assetCode)
        {
            var baseUrl = _configuration["AppUrl"] ?? "https://localhost:5259";
            return $"{baseUrl}/Mobile/ScanAsset/{assetId}";
        }

        public async Task<string> SaveQrCodeAsync(int assetId, string assetCode)
        {
            var url = GenerateAssetQrCode(assetId, assetCode);
            var qrCodeBytes = GenerateQrCode(url);
            
            var uploadsFolder = Path.Combine("wwwroot", "uploads", "qrcodes");
            Directory.CreateDirectory(uploadsFolder);
            
            var fileName = $"asset-{assetId}-{Guid.NewGuid()}.png";
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            await File.WriteAllBytesAsync(filePath, qrCodeBytes);
            
            return $"/uploads/qrcodes/{fileName}";
        }
    }
}
