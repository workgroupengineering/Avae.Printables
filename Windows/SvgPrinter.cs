#if WINDOWS10_0_19041_0_OR_GREATER
using SkiaSharp.Views.Windows;
using Svg.Skia;

namespace Avae.Printables
{
    public class SvgPrinter : PrinterBase
    {
        public SvgPrinter(nint handle, string title, string file)
            : base(handle, title, file)
        {
        }

        protected override void CreatePreview(double printableWidth, double printableHeight)
        {
            var canvas = new SKXamlCanvas
            {
                Width = printableWidth,
                Height = printableHeight
            };
            
            canvas.PaintSurface += (s, e) =>
            {
                using var image = new SKSvg();
                image.Load(file);
                e.Surface.Canvas.DrawPicture(image.Picture, 0, 0);
            };
            canvas.Invalidate(); // still needed to trigger paint

            printPreviewPages.Add(canvas);
        }
    }
}
#endif