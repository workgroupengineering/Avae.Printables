#if GTK
using Gtk;
using SkiaSharp;

namespace Avae.Printables
{
    public class PdfOperation : PrintOperationBase
    {
        private IEnumerable<SKBitmap> images;
        public PdfOperation(string title,string file)
            : base(title)
        {
            images = PDFtoImage.Conversion.ToImages(File.ReadAllBytes(file));
        }

        protected override void SetNbPages()
        {
             NPages = images.Count();
        }

        protected override Task Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight)
        {
            var image = images.ElementAt(page_nr);

            using var img = SKImage.FromBitmap(image);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());

            using var pixbuf = new Gdk.Pixbuf(ms, (int)printableWidth, (int)printableHeight);
            using var cr = context.CairoContext;
            Gdk.CairoHelper.SetSourcePixbuf(cr, pixbuf, 0, 0);
            cr.Paint();
            return Task.CompletedTask;

        }
    }
}
#endif