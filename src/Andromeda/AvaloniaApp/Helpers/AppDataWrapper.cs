using static Andromeda.Core.FSharp.DomainTypes;

namespace Andromeda.AvaloniaApp.Helpers {
    /**
    This is a wrapper class for appdata to ensure,
    that there exists only one reference, which can be changed.
    */
    public class AppDataWrapper {
        public AppData AppData { get; set; }
    }
}
