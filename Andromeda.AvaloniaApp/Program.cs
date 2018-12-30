using Avalonia;
using Avalonia.Logging.Serilog;
using CefGlue.Avalonia;
using Couchbase.Lite;
using Couchbase.Lite.Logging;
using System;

using Andromeda.AvaloniaApp.ViewModels.Windows;
using Andromeda.AvaloniaApp.Windows;

namespace Andromeda.AvaloniaApp
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Initialise Couchbase Lite
            Couchbase.Lite.Support.NetDesktop.Activate();
            Database.SetLogLevel(LogDomain.All, LogLevel.None);

            BuildAvaloniaApp(args).Start<MainWindow>(() => new MainWindowViewModel());
        }

        public static AppBuilder BuildAvaloniaApp(string[] args) {
            AppBuilder builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .UseReactiveUI()
                .LogToDebug();

            // Use CefGlue only on Windows for now...
            //builder = builder.ConfigureCefGlue(args);
            return builder;
        }
    }
}
