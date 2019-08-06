using Andromeda;
using Andromeda.Core.FSharp;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Windows;
using Andromeda.AvaloniaApp.Windows;

using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels {
    public class ViewModelBase : ReactiveObject {
        public AppDataWrapper AppDataWrapper { get; private set; }
        protected DomainTypes.AppData AppData {
            get => this.AppDataWrapper.AppData;
        }

        private readonly IList<SubViewModelBase> children = new List<SubViewModelBase>();
        public IList<SubViewModelBase> Children { get => this.children; }

        public Control Control { get; }

        public ViewModelBase(Control control) : this(control, null) {
            // Nothing to do here
        }

        public ViewModelBase(Control control, AppDataWrapper appDataWrapper) {
            this.Control = control;

            if (appDataWrapper != null) {
                this.AppDataWrapper = appDataWrapper;
            }
            else {
                this.AppDataWrapper = new AppDataWrapper();
                this.SetAppData(Core.FSharp.AppData.loadAppData());
                this.SetAppData(searchInstalled(this.AppData));
                if (this.AppData.authentication.IsNoAuth) {
                    // Authenticate
                    var window = new AuthenticationWindow();
                    window.DataContext = new AuthenticationWindowViewModel(window, this.AppDataWrapper);
                    window.ShowDialog(this.GetParentWindow());
                }
            }
        }

        public virtual Window GetParentWindow() {
            return this.Control is Window ? (Window)this.Control : null;
        }

        protected virtual void SetAppData(DomainTypes.AppData appData) {
            this.AppDataWrapper.AppData = appData;
            Core.FSharp.AppData.saveAppData(this.AppData);
        }

        public IList<T> GetChildrenOfType<T>() {
            return this.children
                .Where(child => typeof(T).IsInstanceOfType(child))
                .Cast<T>()
                .ToList();
        }
    }
}
