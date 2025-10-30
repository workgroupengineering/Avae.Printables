#if GTK
using Avalonia.X11.Interop;
using Gtk;
using SkiaSharp;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using WebKit;
using Path = System.IO.Path;

namespace Avae.Printables
{  
    public class PrintingService : PrintingBase, IPrintingService
    {
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

        public Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            Gtk.Application.Init();
            var printers = new List<PrintablePrinter>();
            
            Printer.EnumeratePrinters(new PrinterFunc(p =>
            {
                var moq = new PrintablePrinter()
                {
                   Name= p.Name
                };
                printers.Add(moq);
                return false;
            }), false);
            return Task.FromResult(printers.AsEnumerable());
        }

        // P/Invoke for webkit_web_frame_print_full
        [DllImport("libwebkitgtk-1.0.so.0")]
        static extern void webkit_web_frame_print_full(IntPtr webFrame, IntPtr printOperation, int action);

        // GTK_PRINT_OPERATION_ACTION enum
        const int GTK_PRINT_OPERATION_ACTION_PRINT_DIALOG = 0;
        const int GTK_PRINT_OPERATION_ACTION_EXPORT = 1;

        private static Task<string> HtmlToPdf(string file)
        {
            var temp = Path.GetTempPath() + "temp.pdf";
            float A4_WIDTH = 595.28f;
            float A4_HEIGHT = 841.89f;
            // Must run on GTK main thread
            _ = GtkInteropHelper.RunOnGlibThread(() =>
            {
                var view = new WebView()
                {
                    WidthRequest = (int)A4_WIDTH,
                    HeightRequest = (int)A4_HEIGHT
                };
                LoadChangedHandler? handler = null!;
                view.LoadChanged += handler = (s, e) =>
                {
                    if (e.LoadEvent == LoadEvent.Finished)
                    {
                        view.LoadChanged -= handler;
                        var pdf = Path.GetTempPath() + "temp.pdf";
                        var printOp = new WebKit.PrintOperation(view);
                        printOp.PrintSettings = new PrintSettings()
                        {
                            OutputBin = pdf
                        };
                        IntPtr framePtr = view.Handle;
                        // Call low-level print function
                        webkit_web_frame_print_full(framePtr, printOp.Handle, GTK_PRINT_OPERATION_ACTION_EXPORT);
                    }
                };
                view.LoadUri("file://" + file);
                return true;
            });
            return Task.FromResult(temp);
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
