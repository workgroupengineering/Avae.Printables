using Avalonia;
using Microsoft.Extensions.DependencyInjection;

#if ANDROID
using Activity = Android.App.Activity;
using Context = Android.Content.Context;
#endif

namespace Avae.Printables
{

    public static class Extensions
    {
#if ANDROID
        public static AppBuilder UsePrintables(this AppBuilder builder, Activity activity, Context context)
#elif WINDOWS10_0_19041_0_OR_GREATER
        public static AppBuilder UsePrintables(this AppBuilder builder, bool isHybrid = false)
#else
        public static AppBuilder UsePrintables(this AppBuilder builder)
#endif
        {
#if ANDROID
            Printable.SetDefault(new PrintingService(activity, context));
#elif BROWSER || MACOS || IOS || GTK
            Printable.SetDefault(new PrintingService());
#elif WINDOWS10_0_19041_0_OR_GREATER
            Printable.SetDefault(new PrintingService(isHybrid));
#endif

            return builder;
        }
    }

    public static class Printable
    {
        static IPrintingService defaultImplementation;

        /// <summary>
        /// Provides the default implementation for static usage of this API.
        /// </summary>
        public static IPrintingService Default =>
            defaultImplementation;

        internal static void SetDefault(IPrintingService implementation) =>
            defaultImplementation = implementation;

        public static Task PrintAsync(IEnumerable<Visual> visuals, string jobTitle = "Title")
        {
            return Default.PrintAsync(visuals, jobTitle);
        }
        public static Task PrintAsync(string file, Stream? stream = null, string jobTitle = "Title")
        {
            return Default.PrintAsync(file, stream, jobTitle);
        }

        public static Task PrintAsync(PrintablePrinter printer, string file)
        {
            return Default.PrintAsync(printer, file);
        }

        public static Task PrintAsync(string jobTitle = "Title")
        {
            return Default.PrintAsync(jobTitle);
        }

        public static Task<IEnumerable<PrintablePrinter>> GetPrintersAsync()
        {
            return Default.GetPrintersAsync();
        }
    }
}
