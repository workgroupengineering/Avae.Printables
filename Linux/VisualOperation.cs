#if GTK
using Avalonia;
using Avalonia.Skia.Helpers;
using Gtk;
using SkiaSharp;

namespace Avae.Printables
{
    public class VisualOperation : PrintOperationBase
    {
        private IEnumerable<Visual> visuals;
        public VisualOperation(string title, IEnumerable<Visual> visuals)
            : base(title)
        {
            this.visuals = visuals;
        }

        protected override void SetNbPages()
        {
            NPages = visuals.Count();
        }
        protected async override void Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight)
        {
            var visual = visuals.ElementAt(page_nr);

            using var img = await VisualHelper.Render(visual, printableWidth, printableHeight, DrawingContextHelper.RenderAsync);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());

            using var pixbuf = new Gdk.Pixbuf(ms, (int)printableWidth, (int)printableHeight);
            using var cr = context.CairoContext;
            Gdk.CairoHelper.SetSourcePixbuf(cr, pixbuf, 0, 0);
            cr.Paint();
        }
    }
}
#endif