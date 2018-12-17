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
    public class SubViewModelBase : ViewModelBase
    {
        protected ViewModelBase Parent {
            set; get;
        }

        public SubViewModelBase(ViewModelBase parent) : base(parent.AppDataWrapper)
        {
            this.Parent = parent;
        }
    }
}
