using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.IO;

namespace Avae.Printables
{
    public interface IPrintingService
    {
        internal Visual GetVisual()
        {
            return Avalonia.Application.Current!.ApplicationLifetime
            is ISingleViewApplicationLifetime singleViewApplicationLifetime ? singleViewApplicationLifetime.MainView! :
            (Avalonia.Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!;
        }        

        Task PrintVisualAsync(string title = "Title")
        {
            var visual = GetVisual();
            return PrintVisualsAsync([visual], title);
        }

        Task PrintVisualsAsync(IEnumerable<Visual> visuals, string title = "Title");
        Task PrintAsync(string file, Stream? stream = null, string title = "Title");
        Task<bool> PrintAsync(PrintablePrinter printer, string file, string ouputfilename = "")
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            return Task.FromResult(Enumerable.Empty<PrintablePrinter>());
        }
    }

    public class PrintablePrinter
    {
        public string? Name { get; set; }
    }
}
