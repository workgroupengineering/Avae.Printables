#if WINDOWS10_0_19041_0_OR_GREATER
using Avalonia;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using SkiaSharp.Views.Windows;

namespace Avae.Printables
{
    public class VisualPrinter : PrinterBase
    {
        public VisualPrinter(nint handle, string title, IEnumerable<Visual> visuals)
            : base(handle, title, visuals)
        {
        }

        protected override void CreatePreview(double printableWidth, double printableHeight)
        {
            foreach (var visual in visuals)
            {
                var canvas = new SKXamlCanvas
                {
                    Width = printableWidth,
                    Height = printableHeight
                };
                canvas.PaintSurface += async (s, e) =>
                {
                    using var img = await VisualHelper.Render(visual, printableWidth, printableHeight, DrawingContextHelper.RenderAsync);
                    e.Surface.Canvas.DrawImage(img, new SKRect(0, 0, (float)printableWidth, (float)printableHeight));
                };
                canvas.Invalidate(); // still needed to trigger paint
                printPreviewPages.Add(canvas);
            }
        }
    }
}
#endif