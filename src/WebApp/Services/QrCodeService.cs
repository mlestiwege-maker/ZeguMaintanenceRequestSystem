using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;

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
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var pixelsPerModule = Math.Max(1, size / 40);
            return qrCode.GetGraphic(pixelsPerModule);
        }

        public string GenerateAssetQrCode(int assetId, string assetCode)
        {
            var baseUrl = _configuration["AppUrl"] ?? "https://localhost:5259";
            return $"{baseUrl}/Mobile/Scan/{assetId}";
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
