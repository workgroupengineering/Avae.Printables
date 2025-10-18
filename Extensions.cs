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
        public static void UsePrinting(this IServiceCollection services, Activity activity, Context context)
#else
        public static void UsePrinting(this IServiceCollection services)
#endif
        {
#if ANDROID
            var service = new PrintingService(activity, context);
            services.AddSingleton<IPrintingService>(service);
            Printable.SetDefault(service);
#else
            var service = new PrintingService();
            services.AddSingleton<IPrintingService>(service);
            Printable.SetDefault(service);
#endif
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


        public static Task Print(string title, IEnumerable<Visual> visuals)
        {
            return Default.Print(title, visuals);
        }
        public static Task Print(string title, string file)
        {
            return Default.Print(title, file);
        }
        public static Task Print(IPrinter printer, string file)
        {
            return Default.Print(printer, file);
        }
        public static IEnumerable<IPrinter> GetPrinters()
        {
            return Default.GetPrinters();
        }
    }
}
