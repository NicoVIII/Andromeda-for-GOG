using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Reflection;

using Andromeda.AvaloniaApp.Converter;

namespace Andromeda.AvaloniaApp.Windows {
    public class MainWindow : Window {
        public MainWindow() {
            this.InitializeComponent();
            this.AttachDevTools(); // TODO: remove
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            var assembly = Assembly.GetExecutingAssembly();
            using (Stream resource = assembly.GetManifestResourceStream("Andromeda.AvaloniaApp.Assets.logo.ico"))
                this.Icon = new WindowIcon(resource);
        }
    }
}
