using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;
using System.Drawing;
using System.Drawing.Imaging;

namespace TeaOnlineShop.Services
{
    public class QRCodeService
    {
        public byte[] GenerateQRCode(string data)
        {
            // Create a QR code writer
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 1,
                    ErrorCorrection = ErrorCorrectionLevel.Q
                }
            };

            // Generate the QR code
            var pixelData = writer.Write(data);

            // Convert to bitmap
            using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using var ms = new MemoryStream();
            
            // Copy the pixel data to the bitmap
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                
            try
            {
                // Copy the pixel data
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                // Unlock the bitmap data
                bitmap.UnlockBits(bitmapData);
            }
            
            // Save to memory stream
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        
        public string GenerateUniqueQRCodeData(string supplierCode)
        {
            // Generate a unique identifier for the QR code
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // Format: TEASHOP-{supplierCode}-{timestamp}-{random}
            return $"TEASHOP-{supplierCode}-{timestamp}-{guid}";
        }

        public string GenerateInventoryQRCode(string teaType, string grade, string batchNumber)
        {
            // Format for inventory items: INV-{teaType}-{grade}-{batchNumber}-{timestamp}-{random}
            string timestamp = DateTime.Now.ToString("yyMMdd");
            string randomCode = new Random().Next(1000, 9999).ToString();
            
            // Clean up input for QR code
            teaType = teaType.Replace(" ", "").ToUpper().Substring(0, Math.Min(3, teaType.Length));
            grade = grade.Replace(" ", "").ToUpper().Substring(0, Math.Min(3, grade.Length));
            
            return $"INV-{teaType}-{grade}-{batchNumber}-{timestamp}-{randomCode}";
        }
        
        public string GetDataUrl(byte[] qrCodeBytes)
        {
            string base64String = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{base64String}";
        }
        
        public bool IsValidInventoryQRCode(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
                return false;
                
            return qrCode.StartsWith("INV-") && qrCode.Split('-').Length == 6;
        }
        
        public Dictionary<string, string> ParseInventoryQRCode(string qrCode)
        {
            var result = new Dictionary<string, string>();
            
            if (!IsValidInventoryQRCode(qrCode))
                return result;
                
            var parts = qrCode.Split('-');
            
            result.Add("Type", "Inventory");
            result.Add("TeaType", parts[1]);
            result.Add("Grade", parts[2]);
            result.Add("BatchNumber", parts[3]);
            result.Add("DateCode", parts[4]);
            result.Add("RandomCode", parts[5]);
            
            return result;
        }
    }
} 