using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

using static Andromeda.Core.FSharp.AppData;
using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected AppDataWrapper AppDataWrapper { get; private set; }
        protected AppData AppData
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

        public ViewModelBase(AppDataWrapper appDataWrapper = null)
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
    }
}
