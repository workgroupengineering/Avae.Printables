using Avalonia;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using System.IO;

namespace Avae.Printables
{
    public class PrintingBase
    {
        public const float A4_WIDTH = 595.28f;
        public const float A4_HEIGHT = 841.89f;

        public const float LETTER_WIDTH = 612f;  // Letter portrait
        public const float LETTER_HEIGHT = 792f;

        public delegate Task<string> ConversionDelegate(string file);

        private static Dictionary<string, ConversionDelegate> _conversions = new Dictionary<string, ConversionDelegate>()
        {
            { ".jpeg" , ImageHelper.ConvertToPdf },
            { ".bmp" , ImageHelper.ConvertToPdf },
            { ".jpg" , ImageHelper.ConvertToPdf },
            { ".png" , ImageHelper.ConvertToPdf },
            {".ico" , ImageHelper.ConvertToPdf },
            {".gif" , ImageHelper.ConvertToPdf },
            {".pdf" , (file) => Task.FromResult(file) }
        };

        public Dictionary<string, ConversionDelegate> Conversions
        {
            get { return _conversions; }
        }

        public static string GetTempPdf()
        {
            return Path.Combine(Path.GetTempPath(),
                Printable.GENERATION == GENERATION.GUID ?
                Guid.NewGuid().ToString() :
                "temp.pdf");

        }        

        public static async Task<string> CreatePdf_A4Base64(IEnumerable<Visual> visuals)
        {
            using var file = new MemoryStream();
            using var doc = SKDocument.CreatePdf(file);
            await RenderDocument(doc, visuals, A4_WIDTH, A4_HEIGHT);
            return Convert.ToBase64String(file.ToArray());
        }

        public static async Task<MemoryStream> CreatePdf_A4Stream(IEnumerable<Visual> visuals)
        {
            var stream = new MemoryStream();
            using var doc = SKDocument.CreatePdf(stream);
            await RenderDocument(doc, visuals, A4_WIDTH, A4_HEIGHT);
            stream.Position = 0;
            return stream;
        }

        private static async Task RenderDocument(SKDocument document, IEnumerable<Visual> visuals, double pageWidth, double pageHeight, SKRect? rect = null)
        {
            foreach (var visual in visuals)
            {
                using var canvas = document.BeginPage((float)pageWidth, (float)pageHeight);
                using var image = await VisualHelper.MeasureArrange(visual, pageWidth, pageHeight, DrawingContextHelper.RenderAsync);
                if(rect.HasValue)
                    canvas.DrawImage(image, new SKRect(0, 0, (float)pageWidth, (float)pageHeight));
                else
                    canvas.DrawImage(image, 0, 0);
                document.EndPage();
            }

            document.Close();
        }

        private static async Task<string> CreatePdf(IEnumerable<Visual> visuals, double pageWidth, double pageHeight, SKRect? rect = null)
        {
            var temp = GetTempPdf();
            using var doc = SKDocument.CreatePdf(temp);
            await RenderDocument(doc, visuals, pageWidth, pageHeight, rect);
            return temp;
        }

        public static Task<string> CreatePdf_A4(IEnumerable<Visual> visuals, SKRect? rect = null)
        {
            return CreatePdf(visuals, A4_WIDTH, A4_HEIGHT, rect);
        }

        public static Task<string> CreatePdfAsStream_A4(IEnumerable<Visual> visuals)
        {
            return CreatePdf(visuals, A4_WIDTH, A4_HEIGHT);
        }

        public static Task<string> CreatePdf_LETTER(IEnumerable<Visual> visuals)
        {
            return CreatePdf(visuals, LETTER_WIDTH, LETTER_HEIGHT);
        }
    }
}
