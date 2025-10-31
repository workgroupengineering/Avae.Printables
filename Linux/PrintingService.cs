#if GTK
using Avalonia.X11.Interop;
using Gtk;
using SkiaSharp;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Weasyprint.Wrapped;
using WebKit;
using Path = System.IO.Path;
using Printer = Weasyprint.Wrapped.Printer;

namespace Avae.Printables
{  
    public class PrintingService : PrintingBase, IPrintingService
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private static Printer? printer;
        public static async Task<Printer> WeasyPrinter()
        {
            if (printer == null)
            {
                await _semaphore.WaitAsync();
                if (printer == null)
                {
                    var config = new ConfigurationProvider();
                    var asset = config.GetAsset();
                    if (!File.Exists(asset))
                    {
                        using var client = new HttpClient();
                        using var response = await client.GetAsync("https://github.com/berthertogen/weasyprint.wrapped/releases/latest/download/standalone-linux-64.zip");
                        if(response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception("Failed to download WeasyPrint asset.", new Exception(response.ReasonPhrase));
                        }
                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var zip = File.OpenWrite(asset);
                        stream.CopyTo(zip);
                    }
                    
                    printer = new Printer(config);
                    await printer.Initialize();
                }
                _semaphore.Release();
            }

            return printer;
        }

        public PrintingService()
        {
            Conversions.Add(".htm", HtmlToPdf);
            Conversions.Add(".html", HtmlToPdf);
        }

        public delegate Task<PrintOperationBase> PrintDelegate(string title, string file);


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

        private static Task<PrintOperationBase> PrintPdf(string title, string file)
        {
            return Task.FromResult<PrintOperationBase>(new PdfOperation(title, file));
        }

        private static Task<PrintOperationBase> PrintImage(string title, string file)
        {
            return Task.FromResult<PrintOperationBase>(new ImageOperation(title, file));
        }

        public async Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            var printers = new List<PrintablePrinter>();
            var psi = new ProcessStartInfo
            {
                FileName = "lpstat",
                Arguments = "-e",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi)!;
            string output = process.StandardOutput.ReadToEnd();
            await process.WaitForExitAsync();

            // Extract printer names
            foreach(var name in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                printers.Add(new PrintablePrinter()
                {
                    Name = name
                });
            }

            return printers.AsEnumerable();
        }

        private async static Task<string> HtmlToPdf(string file)
        {
            file = WebUtility.UrlDecode(file).Replace("file://", string.Empty);
            var temp = GetTempPdf();
            var printer = await WeasyPrinter();
            string css = string.Empty;
            var directory = Path.GetDirectoryName(file);
            if(!string.IsNullOrWhiteSpace(directory))
            {
                directory = WebUtility.UrlDecode(directory);
                var cssFile = Directory.EnumerateFiles(directory, "*.css").FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(cssFile))
                {
                    css = $"-s {cssFile}";
                }
            }
            var result = await printer.Print(File.ReadAllText(file), css);
            File.WriteAllBytes(temp, result.Bytes);
            return temp;
        }

        public static Task<PrintOperationBase> PrintHtml(string title, string file)
        {
            float A4_WIDTH = 595.28f;
            // Must run on GTK main thread
            _ = GtkInteropHelper.RunOnGlibThread(() =>
            {
                var view = new WebView()
                {                    
                    WidthRequest = (int)A4_WIDTH
                };
                LoadChangedHandler? handler = null!;
                view.LoadChanged += handler = (s, e) =>
                {
                    if (e.LoadEvent == LoadEvent.Finished)
                    {
                        view.LoadChanged -= handler;                        
                        var op = new WebKit.PrintOperation(view);
                        op.RunDialog();
                    }
                };

                view.LoadUri("file://" + file);
                return true;
            });

            return Task.FromResult<PrintOperationBase>(null!);
        }

        public async Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            Gtk.Application.Init();

            file = file.Replace("file://", string.Empty);
            var decodedPath = WebUtility.UrlDecode(file);
            var ext = Path.GetExtension(decodedPath).ToLower();
            
            PrintOperationBase? operation = null!;
            if (Entries.TryGetValue(ext, out var entry))
            {
                operation = await entry(title, decodedPath);
            }
            else
            {
                operation = new TxtOperation(title, decodedPath);
            }

            if (operation != null)
            {
                operation.Run(PrintOperationAction.PrintDialog, null);
            }
        }

        public Task PrintVisualsAsync(IEnumerable<Avalonia.Visual> visuals, string title = "Title")
        {
            Gtk.Application.Init();
            var operation = new VisualOperation(title, visuals);
            operation.Run(PrintOperationAction.PrintDialog, null);
            return Task.CompletedTask;
        }

        public async Task<bool> PrintAsync(PrintablePrinter printer, string file, string ouputfilename = "Silent job")
        {
            if (printer == null)
                throw new ArgumentNullException(nameof(printer));

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
    }
}
#endif
