#if WINDOWS10_0_19041_0_OR_GREATER
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using System.Diagnostics;
namespace Avae.Printables
{
    public class HtmlPrinter : PrinterBase
    {

        public HtmlPrinter(nint handle, string title, string file)
        : base(handle, title, file)
        {

        }

        protected override async Task CreatePreview(double printableWidth, double printableHeight)
        {
            try
            {
                WebView2 w = new WebView2();
                await w.EnsureCoreWebView2Async();

                var tcs = new TaskCompletionSource();

                file = "file:///" + file;
                w.Source = new Uri(file);
                //w.CoreWebView2.Navigate(file);
                string pdfPath = string.Empty;
                w.CoreWebView2.DOMContentLoaded += async (sender, e) =>
                {
                    var path = Path.Combine(Path.GetTempPath(), "preview.pdf");
                    var settings = w.CoreWebView2.Environment.CreatePrintSettings();
                    settings.ShouldPrintBackgrounds = true;
                    settings.MarginBottom =
                    settings.MarginLeft =
                    settings.MarginRight =
                    settings.MarginTop = 0;

                    await w.CoreWebView2.PrintToPdfAsync(path, settings);
                    pdfPath = Convert.ToBase64String(File.ReadAllBytes(path));
                    tcs.SetResult();
                };
                await tcs.Task;

                var images = PDFtoImage.Conversion.ToImages(pdfPath);
                foreach (var bitmap in images)
                {
                    var canvas = new SKXamlCanvas
                    {
                        Width = printableWidth,
                        Height = printableHeight
                    };

                    canvas.PaintSurface += (s, e) =>
                    {
                        using var img = SKImage.FromBitmap(bitmap);
                        e.Surface.Canvas.DrawImage(img, new SKRect(0, 0, (float)printableWidth, (float)printableHeight));
                    };
                    canvas.Invalidate(); // still needed to trigger paint
                    printPreviewPages.Add(canvas);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debug.WriteLine("Ensure running in x64");
            }
        }
    }
}
#endif
