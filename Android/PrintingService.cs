#if ANDROID
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.Print;
using Android.Print.Pdf;
using Android.Text;
using Android.Webkit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Paint = Android.Graphics.Paint;

namespace Avae.Printables
{
    public class PrintingService : PrintingBase, IPrintingService
    {
        Activity activity;
        Context context;
        public PrintingService(Activity activity, Context context)
        {
            this.activity = activity;
            this.context = context;
        }

        public delegate Task PrintDelegate(string title, string file);

        private Dictionary<string, PrintDelegate> _entries = new Dictionary<string, PrintDelegate>()
        {
            { ".pdf", PrintPdf },
            {    ".jpeg" ,PrintImage },
             {   ".bmp" , PrintImage },
              {  ".jpg" , PrintImage },
               { ".png" , PrintImage },
                {".ico" , PrintImage },
                {".gif" , PrintImage },
                {".htm" , PrintHtml },
                {".html" , PrintHtml },
        };
        public Dictionary<string, PrintDelegate> Entries
        {
            get
            {
                return _entries;
            }

        }

        private static Task PrintImage(string title, string file)
        {
            var service = Printable.Default as PrintingService;
            if (service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");
            return service.PrintAsync(BitmapToPdf(file), null, title);
        }

        private static async Task PrintTxt(string title, string file)
        {
            const float pageWidth = 612f;  // Letter portrait
            const float pageHeight = 792f;
            var text = File.ReadAllText(file);
            text = text.Replace("\r\n", "\n");
            var CharactersPerPage = TxtHelper.PaginateTextByParagraphs(text, (float)pageWidth, (float)pageHeight);
            var visuals = new List<Visual>();
            foreach (var s in CharactersPerPage)
            {
                var textBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Width = pageWidth,
                    Height = pageHeight,
                    Text = s
                };

                visuals.Add(textBlock);
            }
            var service = Printable.Default as PrintingService;
            if (service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");

            await service.PrintVisualsAsync(visuals, title);
        }

        public static Task PrintHtml(string title, string file)
        {
            var service = Printable.Default as PrintingService;
            if(service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");
            
            var activity = service.activity;
            var context = service.context;

            var webView = new WebView(context);
            webView.LoadDataWithBaseURL(null, File.ReadAllText(file), "text/html", "utf-8", null);
            var printManager = context?.GetSystemService(Context.PrintService) as PrintManager;
            var printAdapter = webView.CreatePrintDocumentAdapter("MyHTMLDocument");
            printManager?.Print(title, printAdapter, null);
            return Task.CompletedTask;
        }

        private static Task PrintPdf(string title, string file)
        {
            var service = Printable.Default as PrintingService;

            if (service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");
            var activity = service.activity;

            var printManager = activity?.GetSystemService(Context.PrintService) as PrintManager;

            // Now we can use the preexisting print helper class
            var adapter = new PrintAdapter(file);

            printManager?.Print(title, adapter, null);

            return Task.CompletedTask;
        }

        public async Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            var ext = System.IO.Path.GetExtension(file).ToLower();
            if(Entries.TryGetValue(ext,out var task))
            {
                await task(title, file);
            }
            else
            {
                await PrintTxt(title, file);
            }
        }

        private static string BitmapToPdf(string file)
        {
            using var bitmap = BitmapFactory.DecodeFile(file);
            if(bitmap == null)
                throw new FileNotFoundException("Could not decode image file.", file);

            var service = Printable.Default as PrintingService;
            if (service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");
            using PrintedPdfDocument pdf = new PrintedPdfDocument(service.context,
            new PrintAttributes.Builder()
                    .SetMediaSize(PrintAttributes.MediaSize.IsoA4) // A4 size
                    .SetMinMargins(new PrintAttributes.Margins(0, 0, 0, 0))
                    .Build());

            using var pageInfo = new PdfDocument.PageInfo.Builder(bitmap.Width, bitmap.Height, 1).Create();
            using var page = pdf.StartPage(pageInfo);

            // Draw the bitmap on the page
            var canvas = page?.Canvas;
            var paint = new Paint(PaintFlags.FilterBitmap);
            canvas?.DrawBitmap(bitmap, 0, 0, paint);

            pdf.FinishPage(page);

            var temp = GetTempPdf();
            using var fs = new FileStream(temp, FileMode.Create);
            pdf.WriteTo(fs);
            return temp;
        }

        public async Task PrintVisualsAsync(IEnumerable<Visual> visuals, string title = "Title")
        {
            await PrintAsync(await CreatePdf_LETTER(visuals), null, title);
        }
    }
}
#endif
