using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Andromeda.AvaloniaApp.ViewModels;
using Andromeda.AvaloniaApp.ViewModels.Widgets;

namespace Andromeda.AvaloniaApp.Widgets
{
    public class DownloadWidget : UserControl
    {
        public DownloadWidget()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
