#if WINDOWS10_0_19041_0_OR_GREATER
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Hosting;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using Windows.Data.Pdf;
using Windows.Storage;

namespace Avae.Printables
{
    public class PrintingService : PrintingBase, IPrintingService
    {
        public delegate Task<PrinterBase> PrintDelegate(string title, string file);
        

        private Dictionary<string, PrintDelegate> _entries = new Dictionary<string, PrintDelegate>()
        {
            { ".pdf", PrintPdf },
            { ".jpeg" ,PrintImage },
            { ".bmp" , PrintImage },
            { ".jpg" , PrintImage },
            { ".png" , PrintImage },
            { ".ico" , PrintImage },
            { ".gif" , PrintImage },
            { ".htm" , PrintHtml },
            { ".html" , PrintHtml },
        };
        public Dictionary<string, PrintDelegate> Entries
        {
            get
            {
                return _entries;
            }
        }

        public PrintingService(bool isHybrid)
        {
            if (!isHybrid)
            {
                DispatcherQueueController.CreateOnCurrentThread();

                WindowsXamlManager.InitializeForCurrentThread();
            }

            Conversions.Add(".htm" , HtmlHelper.ConvertToPdf);
            Conversions.Add(".html", HtmlHelper.ConvertToPdf);
        }

        public Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            var printers = new List<PrintablePrinter>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(new PrintablePrinter()
                {
                    Name = printer
                });
            }
            return Task.FromResult(printers.AsEnumerable());
        }

        private static Task<PrinterBase> PrintPdf(string title, string file)
        {
            return Task.FromResult<PrinterBase>(new PdfPrinter(GetActiveWindow(), title, file));
        }

        private static Task<PrinterBase> PrintImage(string title, string file)
        {
            return Task.FromResult<PrinterBase>(new ImagePrinter(GetActiveWindow(), title, file));
        }

        private static async Task<PrinterBase> PrintHtml(string title, string file)
        {
            if (Printable.RENDERING == RENDERING.EDGE)
            {
                var printer = new HtmlPrinter(file);
                await printer.ShowPrintUI();
                return null!;
            }
            return new PdfPrinter(GetActiveWindow(), title, await HtmlHelper.ConvertToPdf(file));
        }

        public async Task<bool> PrintAsync(PrintablePrinter printer, string file, string ouputfilename = "Silent job")
        {
            if(printer == null)
                throw new ArgumentNullException(nameof(printer));

            var ext = Path.GetExtension(file).ToLower();
            var service = Printable.Default as PrintingService;
            if (service == null)
                throw new InvalidOperationException("PrintingService is not initialized.");

            if (!service.Conversions.TryGetValue(ext, out var conversion))
                throw new NotSupportedException($"The file extension '{ext}' is not supported for silent printing.");

            var p = LocalPrintServer.GetDefaultPrintQueue();
            var ticket = p.UserPrintTicket.GetXmlStream().ToArray();

            var pdfFile = await StorageFile.GetFileFromPathAsync(await conversion(file));
            var pdfDoc = await PdfDocument.LoadFromFileAsync(pdfFile);

            var spool = new SpoolHelper();
            return spool.Print(printer.Name ?? string.Empty, ouputfilename, ticket, pdfDoc);
        }

        public async Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            var ext = Path.GetExtension(file).ToLower();
            PrinterBase? printer = null!;
            if (Entries.TryGetValue(ext, out var entry))
            {
                printer = await entry(title, file);
            }
            else
            {
                printer = new TxtPrinter(GetActiveWindow(), title, file);
            }

            if (printer != null)
            {
                printer.RegisterForPrinting();
                await printer.ShowPrintUIAsync();
            }
        }
        
        public async Task PrintVisualsAsync(IEnumerable<Visual> visuals, string title = "Title")
        {
            var helper = new VisualPrinter(GetActiveWindow(), title, visuals);
            helper.RegisterForPrinting();
            await helper.ShowPrintUIAsync();
        }

        public static IntPtr GetActiveWindow()
        {
            if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopStyleApplicationLifetime)
            {
                return desktopStyleApplicationLifetime.MainWindow!.TryGetPlatformHandle()!.Handle;
            }
            return IntPtr.Zero;
        }
    }
}
#endif
