using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Windows;
using Andromeda.AvaloniaApp.Windows;

using static Andromeda.Core.FSharp.AppData;
using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels {
    public class ViewModelBase : ReactiveObject {
        public AppDataWrapper AppDataWrapper { get; private set; }
        protected AppData AppData {
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
                this.SetAppData(loadAppData());
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

        protected virtual void SetAppData(AppData appData) {
            this.AppDataWrapper.AppData = appData;
            saveAppData(this.AppData);
        }

        public IList<T> GetChildrenOfType<T>() {
            return this.children
                .Where(child => typeof(T).IsInstanceOfType(child))
                .Cast<T>()
                .ToList();
        }
    }
}
