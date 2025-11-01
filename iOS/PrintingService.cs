#if IOS
using Avalonia;
using SkiaSharp;
using System.Diagnostics;
using WebKit;

namespace Avae.Printables
{
    public class PrintingService :PrintingBase, IPrintingService
    {
        private class MyDelegate : WKNavigationDelegate
        {
            private readonly TaskCompletionSource<bool> _tcs;
            public MyDelegate(TaskCompletionSource<bool> tcs)
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

        public delegate Task PrintDelegate(string title, string file);

        private Dictionary<string, PrintDelegate> _entries = new Dictionary<string, PrintDelegate>()
        {
            { ".pdf", PrintDefault },
            {    ".jpeg" , PrintDefault },
             {   ".bmp" , PrintDefault },
              {  ".jpg" , PrintDefault },
               { ".png" , PrintDefault },
                {".ico" , PrintDefault },
                {".gif" , PrintDefault },
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

        public static async Task PrintHtml(string title, string html)
        {
            var webView = new WKWebView(UIScreen.MainScreen.Bounds, new WKWebViewConfiguration());
            var tcs = new TaskCompletionSource<bool>();

            webView.NavigationDelegate = new MyDelegate(tcs);

            var fileUrl = NSUrl.FromFilename(html); // full path to your HTML file
            var directory = Path.GetDirectoryName(html);
            directory ??= string.Empty;
            webView.LoadFileUrl(fileUrl, NSUrl.FromFilename(directory));
            // Wait for load to finish
            await tcs.Task;

            var printController = UIPrintInteractionController.SharedPrintController;
            var printInfo = UIPrintInfo.PrintInfo;
            printInfo.JobName = title;
            printInfo.OutputType = UIPrintInfoOutputType.General;

            printController.PrintInfo = printInfo;
            printController.PrintFormatter = webView.ViewPrintFormatter; // important

            // 6️⃣ Present print UI
            printController.Present(true, (controller, completed, err) =>
            {
                if (err != null)
                    Console.WriteLine($"❌ Print failed: {err.LocalizedDescription}");
                else if (completed)
                    Console.WriteLine("✅ Document printed successfully");
            });
        }

        public static Task PrintDefault(string title, string file)
        {
            var print = UIPrintInteractionController.SharedPrintController;

            if (print == null)
            {
                Console.WriteLine("Unable to print at this time.");
            }
            else
            {
                var printInfo = UIPrintInfo.PrintInfo;
                printInfo.OutputType = UIPrintInfoOutputType.General;
                printInfo.JobName = title;

                print.PrintInfo = printInfo;
                var url = NSUrl.FromFilename(file);
                Debug.WriteLine($"URL Valid: {url.CheckPromisedItemIsReachable(out _).ToString()}");
                print.PrintingItem = url;
                print.Present(true, (handler, completed, error) =>
                {
                    if (!completed && error != null)
                    {
                        Console.WriteLine(error.LocalizedDescription);
                    }
                });
            }
            return Task.CompletedTask;
        }

        public async Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            var ext = Path.GetExtension(file).ToLower();
            if (Entries.TryGetValue(ext, out var entry))
            {
                await entry(title, file);
            }
            else
            {
                await PrintDefault(title, file);
            }
        }

        public async Task PrintVisualsAsync(IEnumerable<Visual> visuals, string title = "Title")
        {            
            await PrintAsync(await CreatePdfAsync(title, visuals), null, title);
        }

        public Task<string> CreatePdfAsync(string title, IEnumerable<Visual> visuals)
        {
            return CreatePdf_A4(visuals, new SKRect(0, 0, A4_WIDTH, A4_HEIGHT));
        }
    }
}
#endif
