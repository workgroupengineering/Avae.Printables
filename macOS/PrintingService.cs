#if MACOS
using Avalonia;
using Avalonia.Skia.Helpers;
using PdfKit;
using SkiaSharp;

namespace Avae.Printables
{
    public class PrintingService : IPrintingService
    {
        public IEnumerable<IPrinter> GetPrinters()
        {
            return Enumerable.Empty<IPrinter>();
        }

        public Task Print(IPrinter printer, string file)
        {
            throw new NotImplementedException();
        }

        private Task Print(string title, NSView view)
        {
            var printInfo = new NSPrintInfo
            {
                HorizontallyCentered = true,
                VerticallyCentered = true,
                Orientation = NSPrintingOrientation.Portrait,
            };

            var printOperation = NSPrintOperation.FromView(view, printInfo);
            printOperation.ShowsPrintPanel = true;
            printOperation.ShowsProgressPanel = true;
            printOperation.RunOperation();
            view.Dispose();
            return Task.CompletedTask;
        }

        private NSView FromImage(string file)
        {
            var url = NSUrl.FromFilename(file);
            var image = new NSImage(url);
            var cell = new NSImageCell()
            {
                Image = image
            };
            return cell.ControlView;
        }

        private NSView FromPdf(string file)
        {
            return new PdfView() { Document = new PdfDocument(NSUrl.FromFilename(file)) };
        }

        public Task Print(string title, string file)
        {
            return Print(title, Path.GetExtension(file).ToLower() switch
            {
                ".bmp" => FromImage(file),
                ".jpg" => FromImage(file),
                ".jpeg" => FromImage(file),
                ".ico" => FromImage(file),
                ".pdf" => FromPdf(file),
                _ => FromImage(file)
            });
        }

        public async Task Print(string title, IEnumerable<Visual> visuals)
        {
            float A4_WIDTH = 595.28f;
            float A4_HEIGHT = 841.89f;

            var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");

            using var document = new PdfDocument(NSUrl.FromFilename(file));
            var elements = visuals.ToList();
            foreach (var visual in elements)
            {
                using var image = await VisualHelper.Render(visual, A4_WIDTH, A4_HEIGHT, DrawingContextHelper.RenderAsync);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var nsImage = new NSImage(NSData.FromArray(data.ToArray()));
                var page = new PdfPage(nsImage);
                document.InsertPage(page, elements.IndexOf(visual));
            }

            await Print(title, file);
        }
    }
}
#endif
