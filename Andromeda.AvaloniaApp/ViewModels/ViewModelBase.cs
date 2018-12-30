using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Windows;
using Andromeda.AvaloniaApp.Windows;

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Andromeda.Core.FSharp.AppData;
using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        public AppDataWrapper AppDataWrapper { get; private set; }
        protected virtual AppData AppData
        {
            get
            {
                return this.AppDataWrapper.AppData;
            }
            set
            {
                this.AppDataWrapper.AppData = value;
            }
        }

        public readonly IList<SubViewModelBase> children = new List<SubViewModelBase>();

        public ViewModelBase() : this(null) { }
        public ViewModelBase(AppDataWrapper appDataWrapper)
        {
            if (appDataWrapper != null)
            {
                this.AppDataWrapper = appDataWrapper;
            }
            else
            {
                this.AppDataWrapper = new AppDataWrapper();
                this.AppData = loadAppData();
                this.AppData = searchInstalled(this.AppData);
                if (this.AppData.authentication.IsNoAuth)
                {
                    // Authenticate
                    var window = new AuthenticationWindow();
                    window.DataContext = new AuthenticationWindowViewModel(this.AppDataWrapper);
                    window.ShowDialog();
                }
            }
        }

        public IList<T> GetChildrenOfType<T>()
        {
            return this.children
                .Where(child => typeof(T).IsInstanceOfType(child))
                .Cast<T>()
                .ToList();
        }
    }
}
