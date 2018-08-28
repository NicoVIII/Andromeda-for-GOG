using Andromeda.Core.FSharp;
using Andromeda.AvaloniaApp.Windows;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace Andromeda.AvaloniaApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IReactiveList<AppData.InstalledGame.T> installedGames;

        public IReactiveList<AppData.InstalledGame.T> InstalledGames
        {
            get => this.installedGames;
            private set { this.RaiseAndSetIfChanged(ref this.installedGames, value); }
        }

        public ReactiveCommand<Unit, Unit> OpenInstallWindowCommand { get; }

        public MainWindowViewModel() : base()
        {
            this.InstalledGames = new ReactiveList<AppData.InstalledGame.T>(this.AppData.installedGames.ToList());

            // Initialize commands
            OpenInstallWindowCommand = ReactiveCommand.Create(OpenInstallWindow);
        }

        void OpenInstallWindow()
        {
            var installWindow = new InstallWindow();
            installWindow.DataContext = new InstallWindowViewModel(this.AppDataWrapper);
            installWindow.ShowDialog();
        }
    }
}
