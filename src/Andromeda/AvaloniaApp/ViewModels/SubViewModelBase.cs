using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;

using static Andromeda.Core.FSharp.AppData;
using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels {
    public class SubViewModelBase : ViewModelBase {
        protected ViewModelBase Parent {
            set; get;
        }

        public SubViewModelBase(Control control, ViewModelBase parent) : base(control, parent.AppDataWrapper) {
            parent.Children.Add(this);
            this.Parent = parent;
        }

        public override Window GetParentWindow() {
            return this.Control is Window ? (Window)this.Control : base.GetParentWindow();
        }
    }
}
