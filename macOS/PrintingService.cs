#if MACOS
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using PdfKit;
using SkiaSharp;
using System.Diagnostics;
using WebKit;

namespace Avae.Printables
{
    public class PrintingService : PrintingBase, IPrintingService
    {
        public PrintingService()
        {
            Conversions.Add(".htm", HtmlToPdf);
            Conversions.Add(".html", HtmlToPdf);
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

        const float A4_WIDTH = 595.28f;
        const float A4_HEIGHT = 841.89f;
        const string APPLY_CSS = @"
            (function() {
                var style = document.createElement('style');
                style.textContent = `
                    @media print {
                        * {
                            -webkit-print-color-adjust: exact !important;
                            color-adjust: exact !important;
                        }
                    }
                `;
                document.head.appendChild(style);
            })();
        ";

        private class NavigationDelegate : WKNavigationDelegate
        {
            private readonly TaskCompletionSource<bool> _tcs;
            public NavigationDelegate(TaskCompletionSource<bool> tcs)
            {
                _tcs = tcs;
            }

            public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
            {
                // HTML is fully loaded and laid out
                _tcs.TrySetResult(true);
            }

            public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
            {
                _tcs.TrySetResult(false);
            }
        }

        private static Task PrintImage(string title, string file)
        {
            return PrintAsync(title, FromImage(file));
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
            await ((PrintingService)Printable.Default).PrintVisualsAsync(visuals, title);
        }

        public async Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            var printers = new List<PrintablePrinter>();
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/lpstat",
                Arguments = "-p",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi)!;
            string output = process.StandardOutput.ReadToEnd();
            await process.WaitForExitAsync();

            // Extract printer names
            foreach(var name in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Where(l => l.StartsWith("printer "))
                         .Select(l => l.Split(' ')[1]))
            {
                printers.Add(new PrintablePrinter()
                {
                    Name = name
                });
            }

            return printers.AsEnumerable();
        }

        public async Task<bool> PrintAsync(PrintablePrinter printer, string file, string ouputfilename = "Silent job")
        {
            var ext = Path.GetExtension(file).ToLower();
            if (Conversions.TryGetValue(ext, out var convertFunc))
            {
                file = await convertFunc(file);
            }

            ProcessStartInfo pStartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/lp",
                Arguments = $"-d \"{printer.Name}\" \"{Path.GetFullPath(file)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(pStartInfo);
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            await process.WaitForExitAsync();

            Console.WriteLine("STDOUT: " + stdout);
            Console.WriteLine("STDERR: " + stderr);

            return process.ExitCode == 0;
        }

        public static Task PrintAsync(string title, NSView view)
        {
            using var printOperation = NSPrintOperation.FromView(view, NSPrintInfo.SharedPrintInfo);            
            printOperation.ShowsPrintPanel = true;
            printOperation.ShowsProgressPanel = true;
            printOperation.RunOperation();
            view.Dispose();
            return Task.CompletedTask;
        }

        private static NSView FromImage(string file)
        {
            using var url = NSUrl.FromFilename(file);
            using var image = new NSImage(url);
            return new NSImageView()
            {
                Image = image,
                Frame = new CGRect(0, 0, image.Size.Width > A4_WIDTH ? A4_WIDTH : image.Size.Width, image.Size.Height > A4_HEIGHT ? A4_HEIGHT : image.Size.Height),
                ImageScaling = NSImageScale.ProportionallyUpOrDown
            };
        }

        private static async Task<string> HtmlToPdf(string file)
        {
            using var webView = new WKWebView(new CGRect(0, 0, A4_WIDTH, A4_HEIGHT), new WKWebViewConfiguration());
            var tcs = new TaskCompletionSource<bool>();

            webView.NavigationDelegate = new NavigationDelegate(tcs);

            using var fileUrl = NSUrl.FromFilename(file); // full path to your HTML file
            using var baseDir = NSUrl.FromFilename(Path.GetDirectoryName(file)); // directory containing the file

            webView.LoadFileUrl(fileUrl, baseDir);
            // Wait for load to finish
            await tcs.Task;
            await webView.EvaluateJavaScriptAsync(APPLY_CSS);
            using var data = await webView.CreatePdfAsync(new WKPdfConfiguration());
            var pdfPath = Path.GetTempPath() + "temp.pdf";
            using var pdfUrl = NSUrl.FromFilename(pdfPath);
            data.Save(pdfUrl, false);
            return pdfPath;
        }

        public static async Task PrintHtml(string title, string file)
        {
            using var webView = new WKWebView(new CGRect(0, 0, A4_WIDTH, A4_HEIGHT), new WKWebViewConfiguration());
            var tcs = new TaskCompletionSource<bool>();

            webView.NavigationDelegate = new NavigationDelegate(tcs);

            using var fileUrl = NSUrl.FromFilename(file); // full path to your HTML file
            using var baseDir = NSUrl.FromFilename(Path.GetDirectoryName(file)); // directory containing the file

            webView.LoadFileUrl(fileUrl, baseDir);
            // Wait for load to finish
            await tcs.Task;
            await webView.EvaluateJavaScriptAsync(APPLY_CSS);

            using var op = webView.GetPrintOperation(NSPrintInfo.SharedPrintInfo);
            op.PrintInfo.TopMargin =
                op.PrintInfo.LeftMargin =
                op.PrintInfo.RightMargin =
                op.PrintInfo.BottomMargin = 0;
            op.JobTitle = title;
            op.RunOperation();
        }

        public static Task PrintPdf(string title, string file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var data = NSData.FromStream(fs);
            if (data != null)
                return PrintPdf(title, data);

            return Task.CompletedTask;
        }

        public Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            file = file.Replace("file://", string.Empty);

            var ext = Path.GetExtension(file).ToLower();
            if (Entries.TryGetValue(ext, out var task))
            {
                return task(title, file);
            }

            return PrintTxt(title, file);
        }

        public async Task PrintVisualsAsync(IEnumerable<Visual> visuals, string title = "Title")
        {
            using var stream = new MemoryStream();
            using var doc = SKDocument.CreatePdf(stream);

            foreach (var visual in visuals)
            {
                using var canvas = doc.BeginPage(A4_WIDTH, A4_HEIGHT);
                using var image = await VisualHelper.MeasureArrange(visual, A4_WIDTH, A4_HEIGHT, DrawingContextHelper.RenderAsync);
                canvas.DrawImage(image, 0, 0);
                doc.EndPage();
            }

            doc.Close();

            stream.Position = 0;

            using var data = NSData.FromStream(stream);
            if (data != null)
                await PrintPdf(title, data);
        }

        private static Task PrintPdf(string title, NSData data)
        {
            using var pdfDocument = new PdfDocument(data);
            using var view = new PdfView()
            {
                AutoScales = false,
                Document = pdfDocument
            };
            using var window = new NSWindow();
            window.SetContentSize(view.Frame.Size);
            window.ContentView = view;
            
            // Get the main screen frame
            var screenFrame = NSScreen.MainScreen.Frame;

            // Compute the window’s top-left coordinate for centering
            var windowRect = window.Frame;
            nfloat x = (screenFrame.Width - windowRect.Width) / 2;
            nfloat y = (screenFrame.Height + windowRect.Height) / 2;
            // note: y is top-left, so add height

            // Use CascadeTopLeftFromPoint to position it
            var topLeft = new CGPoint(x, y);
            window.CascadeTopLeftFromPoint(topLeft);
            view.Print(NSPrintInfo.SharedPrintInfo, false);

            return Task.CompletedTask;
        }        
    }
}
#endif
