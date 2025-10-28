#if GTK
using Gtk;
using SkiaSharp;

namespace Avae.Printables
{
    public class ImageOperation : PrintOperationBase
    {
        private string file;
        public ImageOperation(string title, string file)
            : base(title)
        {
            this.file = file;
        }
        protected override void SetNbPages()
        {
            // For simplicity, assume 1 page. In a real implementation, analyze the image to determine if multiple pages are needed.
            NPages = 1;
        }
        protected override Task Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight)
        {
            // Implement image rendering logic here using SkiaSharp.
            using var bitmap = SKBitmap.Decode(file);
            using var img = SKImage.FromBitmap(bitmap);
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