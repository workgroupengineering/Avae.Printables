#if ANDROID
using Android.OS;
using Android.Print;
using Java.IO;

namespace Avae.Printables
{
    public class PrintAdapter(string printFileName) : PrintDocumentAdapter
    {
        public override void OnLayout(PrintAttributes oldAttributes, PrintAttributes newAttributes, CancellationSignal cancellationSignal, LayoutResultCallback callback, Bundle extras)
        {
            if (cancellationSignal.IsCanceled)
            {
                callback.OnLayoutCancelled();
                return;
            }

            var pdi = new PrintDocumentInfo.Builder(printFileName)
                .SetContentType(PrintContentType.Document)
                .Build();

            callback.OnLayoutFinished(pdi, true);
        }

        public override void OnWrite(PageRange[] pages, ParcelFileDescriptor destination, CancellationSignal cancellationSignal, WriteResultCallback callback)
        {
            using (var fileOutputStream = new FileOutputStream(destination.FileDescriptor))
            {
                fileOutputStream.Write(System.IO.File.ReadAllBytes(printFileName));
            }
            callback.OnWriteFinished(new[] { PageRange.AllPages });
        }
    }
}
#endif