#if WINDOWS10_0_19041_0_OR_GREATER
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Hosting;
using System.Diagnostics;
using System.Drawing.Printing;
using Path = System.IO.Path;

namespace Avae.Printables
{
    public class PrintingService : IPrintingService<PrinterBase>
    {
        public delegate PrinterBase PrintDelegate(string title, string file);

        private Dictionary<string, PrintDelegate> _entries = new Dictionary<string, PrintDelegate>()
        {
            { ".pdf", (title, file) => new PdfPrinter(GetActiveWindow(), title, file) },
            {    ".jpeg" ,(title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
             {   ".bmp" , (title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
              {  ".jpg" , (title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
               { ".png" , (title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
                {".ico" , (title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
                {".gif" , (title, file) => new ImagePrinter(GetActiveWindow(), title, file) },
                {".htm" , (title, file) => new HtmlPrinter(GetActiveWindow(), title, file) },
                {".html" , (title, file) => new HtmlPrinter(GetActiveWindow(), title, file) },
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
        }

        public Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            var printers = new List<PrintablePrinter>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                var moq = new PrintablePrinter()
                {
                    Name = printer
                };
                printers.Add(moq);
            }
            return Task.FromResult(printers.AsEnumerable());
        }

        public Task PrintAsync(PrintablePrinter printer, string file)
        {
            var startInfo = new ProcessStartInfo(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            process?.StandardInput.WriteLine($"# Get the current default printer\r\n$defaultPrinter = (Get-WmiObject -Query \"SELECT * FROM Win32_Printer WHERE Default = TRUE\").Name\r\n\r\n# Set the desired printer as the default printer\r\n$desiredPrinter = \"Your Printer Name\"\r\n(Get-WmiObject -Query \"SELECT * FROM Win32_Printer WHERE Name = '{printer.Name}'\").SetDefaultPrinter()\r\n\r\n# Print the document\r\nStart-Process -FilePath '{file}' -Verb Print\r\n\r\n# Restore the original default printer\r\n(Get-WmiObject -Query \"SELECT * FROM Win32_Printer WHERE Name = '$defaultPrinter'\").SetDefaultPrinter()\r\n");
            return Task.CompletedTask;
        }



        public async Task PrintAsync(string file, Stream? stream = null, string title = "Title")
        {
            var ext = Path.GetExtension(file).ToLower();
            PrinterBase? printer = null!;
            if (Entries.TryGetValue(ext, out var entry))
            {
                printer = entry(title, file);
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

        public async Task PrintAsync(IEnumerable<Visual> visuals, string title = "Title")
        {
            var helper = new VisualPrinter(GetActiveWindow(), title, visuals);
            helper.RegisterForPrinting();
            await helper.ShowPrintUIAsync();
        }

        public static IntPtr GetActiveWindow()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopStyleApplicationLifetime)
            {
                return desktopStyleApplicationLifetime.MainWindow.TryGetPlatformHandle().Handle;
            }
            return IntPtr.Zero;
        }
    }
}
#endif
