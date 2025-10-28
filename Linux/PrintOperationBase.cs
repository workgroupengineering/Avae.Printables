#if GTK
using Gtk;
using System.Threading.Tasks;

namespace Avae.Printables
{
    public abstract class PrintOperationBase : PrintOperation
    {
        public PrintOperationBase(string title)
        {
            EmbedPageSetup = true;
            ExportFilename = title; 
        }

        protected abstract void SetNbPages();

        protected abstract Task Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight);

        protected override void OnBeginPrint(PrintContext context)
        {
            base.OnBeginPrint(context);

            SetNbPages();
        }

        protected override async void OnDrawPage(PrintContext context, int pageNr)
        {
            base.OnDrawPage(context, pageNr);

            double printableWidth = this.DefaultPageSetup.GetPageWidth(Unit.Points);
            double printableHeight = this.DefaultPageSetup.GetPageHeight(Unit.Points);
            await Draw(context, pageNr, printableWidth, printableHeight);
        }
    }
}
#endif