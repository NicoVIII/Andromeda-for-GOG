using Avalonia;
using Avalonia.Markup.Xaml;

namespace Andromeda.AvaloniaApp {
    public class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
