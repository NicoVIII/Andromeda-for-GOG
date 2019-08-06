using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Andromeda.AvaloniaApp.Windows {
    public class WebWindow : Window {
        public WebWindow() {
            this.InitializeComponent();
            this.AttachDevTools(); // TODO: remove
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
