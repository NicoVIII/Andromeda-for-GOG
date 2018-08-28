using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Andromeda.AvaloniaApp.Windows
{
    public class InstallWindow : Window
    {
        public InstallWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
