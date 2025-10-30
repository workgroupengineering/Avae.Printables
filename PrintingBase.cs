using System.IO;

namespace Avae.Printables
{
    public class PrintingBase
    {
        public static string GetTempPdf()
        {
            return Path.Combine(Path.GetTempPath(),
                Printable.GENERATION == GENERATION.GUID ?
                Guid.NewGuid().ToString() :
                "temp.pdf");

        }

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
    }
}
