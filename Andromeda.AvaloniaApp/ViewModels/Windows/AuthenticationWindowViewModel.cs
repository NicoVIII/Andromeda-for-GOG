using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

using static Andromeda.Core.FSharp.AppData;
using static GogApi.DotNet.FSharp.Authentication;
using static GogApi.DotNet.FSharp.Listing;

namespace Andromeda.AvaloniaApp.ViewModels.Windows
{
    public class AuthenticationWindowViewModel : ViewModelBase
    {
        private string code = "";

        public string Code
        {
            get => this.code;
            private set { this.RaiseAndSetIfChanged(ref this.code, value); }
        }

        public ReactiveCommand<Window, Unit> AuthenticateCommand { get; }

        public AuthenticationWindowViewModel(AppDataWrapper appDataWrapper) : base(appDataWrapper)
        {
            AuthenticateCommand = ReactiveCommand.Create<Window>(Authenticate);
        }

        void Authenticate(Window window)
        {
            this.SetAppData(new AppData(newToken(this.Code), this.AppData.installedGames));
            window.Close();
        }
    }
}
