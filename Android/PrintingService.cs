#if ANDROID
using Android.Content;
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.Print;
using Android.Print.Pdf;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Paint = Android.Graphics.Paint;
using Rect = Avalonia.Rect;
using Size = Avalonia.Size;

namespace Avae.Printables
{
    public class PrintingService(Activity activity, Context context) : IPrintingService
    {
        public IEnumerable<IPrinter> GetPrinters()
        {
            return Enumerable.Empty<IPrinter>();
        }

        public Task Print(IPrinter printer, string file)
        {
            throw new NotImplementedException();
        }

        private async Task PrintTxt(string title, string file)
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
            await Print(title, visuals);
        }

        private Task PrintPdf(string title, string file)
        {
            var printManager = (PrintManager)activity.GetSystemService(Context.PrintService);

            // Now we can use the preexisting print helper class
            var adapter = new PrintAdapter(file);

            printManager?.Print(title, adapter, null);

            return Task.CompletedTask;
        }

        public async Task Print(string title, string file)
        {
            Task task = System.IO.Path.GetExtension(file).ToLower() switch
            {
                ".pdf" => PrintPdf(title, file),
                ".jpeg" => Print(title, BitmapToPdf(file)),
                ".bmp" => Print(title, BitmapToPdf(file)),
                ".jpg" => Print(title, BitmapToPdf(file)),
                ".ico" => Print(title, BitmapToPdf(file)),
                _ => PrintTxt(title, file)
            };
            await task;
        }

        //private string SvgToPdf(string file)
        //{
        //    using var image = new SKSvg();
        //    image.Load(file);
        //    using PrintedPdfDocument pdf = new PrintedPdfDocument(context,
        //    new PrintAttributes.Builder()
        //            .SetMediaSize(PrintAttributes.MediaSize.IsoA4) // A4 size
        //            .SetMinMargins(new PrintAttributes.Margins(0, 0, 0, 0))
        //            .Build());

        //    var pageInfo = new PdfDocument.PageInfo.Builder(bitmap.Width, bitmap.Height, 1).Create();
        //    var page = pdf.StartPage(pageInfo);

        //    // Draw the bitmap on the page
        //    var canvas = page.Canvas;
        //    canvas.DrawPicture(image., 0, 0);

        //    pdf.FinishPage(page);

        //    var path = System.IO.Path.GetTempPath() + "test.pdf";
        //    using var temp = new FileStream(System.IO.Path.GetTempPath() + "test.pdf", FileMode.Create);
        //    pdf.WriteTo(temp);
        //    return path;
        //}

        private string BitmapToPdf(string file)
        {
            var bitmap = BitmapFactory.DecodeFile(file);
            using PrintedPdfDocument pdf = new PrintedPdfDocument(context,
            new PrintAttributes.Builder()
                    .SetMediaSize(PrintAttributes.MediaSize.IsoA4) // A4 size
                    .SetMinMargins(new PrintAttributes.Margins(0, 0, 0, 0))
                    .Build());

            var pageInfo = new PdfDocument.PageInfo.Builder(bitmap.Width, bitmap.Height, 1).Create();
            var page = pdf.StartPage(pageInfo);

            // Draw the bitmap on the page
            var canvas = page.Canvas;
            var paint = new Paint(PaintFlags.FilterBitmap);
            canvas.DrawBitmap(bitmap, 0, 0, paint);

            pdf.FinishPage(page);

            var path = System.IO.Path.GetTempPath() + "test.pdf";
            using var temp = new FileStream(System.IO.Path.GetTempPath() + "test.pdf", FileMode.Create);
            pdf.WriteTo(temp);
            return path;
        }

        public async Task Print(string title, IEnumerable<Visual> visuals)
        {
            const float pageWidth = 612f;  // Letter portrait
            const float pageHeight = 792f;
            var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "print.pdf");

            using var doc = SKDocument.CreatePdf(file);

            foreach (var visual in visuals)
            {
                using var canvas = doc.BeginPage(pageWidth, pageHeight);
                using var image = await VisualHelper.Render(visual, pageWidth, pageHeight, DrawingContextHelper.RenderAsync);
                float scaleX = pageWidth / (float)visual.Bounds.Width;
                float scaleY = pageHeight / (float)visual.Bounds.Height;
                canvas.Scale(scaleX, scaleY);
                canvas.DrawImage(image, 0, 0);

                //canvas.Scale(scaleX, scaleY);
                //var original = visual.Bounds;

                //MeasureAndArrange(visual, pageWidth, pageHeight);

                //var bounds = visual.Bounds;

                //using var canvas = doc.BeginPage(pageWidth, pageHeight);

                //// Compute scale so the visual fills the page exactly
                ////float scaleX = pageWidth / (float)bounds.Width;
                ////float scaleY = pageHeight / (float)bounds.Height;

                ////canvas.Scale(scaleX, scaleY);

                //await DrawingContextHelper.RenderAsync(canvas, visual);

                //MeasureAndArrange(visual, original.Width, original.Height);
                
                doc.EndPage();
            }

            doc.Close();
            await Print(title, file);
        }


        private void MeasureAndArrange(Visual visual, double width, double height)
        {
            if (visual is Layoutable layoutable)
            {
                layoutable.Measure(new Size(width, height));
                layoutable.Arrange(new Rect(0, 0, width, height));
                layoutable.UpdateLayout();
            }
        }
    }
}
#endif
