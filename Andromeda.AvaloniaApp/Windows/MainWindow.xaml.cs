using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Andromeda.AvaloniaApp.Converter;

namespace Andromeda.AvaloniaApp.Windows
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools(); // TODO: remove
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
