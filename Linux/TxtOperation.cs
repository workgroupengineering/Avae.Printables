#if GTK
using Avalonia.Controls;
using Avalonia.Skia.Helpers;
using Gtk;
using SkiaSharp;

namespace Avae.Printables
{
    internal class TxtOperation : PrintOperationBase
    {
        private string file;
        private List<string> pages;
        public TxtOperation(string title, string file)
            : base(title)
        {
            this.file = file;

            float A4_WIDTH = 595.28f;
            float A4_HEIGHT = 841.89f;
            pages = TxtHelper.PaginateTextByParagraphs(File.ReadAllText(file), (float)A4_WIDTH, (float)A4_HEIGHT);
        }

        protected override async void Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight)
        {
            var textblock = new TextBlock()
            {
                FontSize = 12,
                Text = pages[page_nr]
            };

            using var img = await VisualHelper.Render(textblock, printableWidth, printableHeight, DrawingContextHelper.RenderAsync);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());

            using var pixbuf = new Gdk.Pixbuf(ms, (int)printableWidth, (int)printableHeight);
            using var cr = context.CairoContext;
            Gdk.CairoHelper.SetSourcePixbuf(cr, pixbuf, 0, 0);
            cr.Paint();
        }

        protected override void SetNbPages()
        {
            NPages = pages.Count();
        }
    }
}
#endif