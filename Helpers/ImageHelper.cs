using SkiaSharp;

namespace Avae.Printables
{
    internal class ImageHelper
    {
        public static Task<string> ConvertToPdf(string file)
        {
            var temp = PrintingBase.GetTempPdf();
            using (var doc = SKDocument.CreatePdf(temp))
            {
                using var canvas = doc.BeginPage(PrintingBase.A4_WIDTH, PrintingBase.A4_HEIGHT);
                using var bitmap = SKBitmap.Decode(file);
                using var image = SKImage.FromBitmap(bitmap);
                canvas.DrawImage(image, 0, 0);
                doc.EndPage();
                doc.Close();
            }
            return Task.FromResult(temp);
        }
    }
}
