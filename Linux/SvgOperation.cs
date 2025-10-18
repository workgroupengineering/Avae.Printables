#if GTK
using Gtk;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avae.Printables
{
    internal class SvgOperation : PrintOperationBase
    {
        private string file;
        public SvgOperation(string title, string file)
            : base(title)
        {
            this.file = file;
        }
        protected override void SetNbPages()
        {
            // For simplicity, assume 1 page. In a real implementation, analyze the image to determine if multiple pages are needed.
            NPages = 1;
        }
        protected override void Draw(PrintContext context, int page_nr, double printableWidth, double printableHeight)
        {
            if (file.StartsWith("file:///"))
            {
                file = file.Replace("file://", string.Empty);
            }

            using var image =  new SKSvg();
            image.Load(file);
            using var img = SKImage.FromPicture(image.Picture, new SKSizeI((int)800, (int)printableHeight));
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