using Andromeda.Core.FSharp;
using Avalonia.Controls;
using DynamicData;
using Mono.Unix.Native;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Widgets;
using Andromeda.AvaloniaApp.Windows;

namespace Andromeda.AvaloniaApp.ViewModels.Windows
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string version = "v0.3.0-alpha.3";
        public string Version { get => this.version; }

        private IReactiveList<AppData.InstalledGame.T> installedGames;
        public IReactiveList<AppData.InstalledGame.T> InstalledGames
        {
            get => this.installedGames;
            set
            {
                this.RaiseAndSetIfChanged(ref this.installedGames, value);
            }
        }
        public IReactiveList<DownloadStatus> Downloads
        {
            get => this.DownloadWidgetVM.Downloads;
        }

        public ReactiveCommand<Unit, Unit> OpenInstallWindowCommand { get; }
        public ReactiveCommand<string, Unit> StartGameCommand { get; }
        public ReactiveCommand<Unit, Unit> UpgradeAllGamesCommand { get; }

        public DownloadWidgetViewModel DownloadWidgetVM { get; }

        public MainWindowViewModel(Control control) : base(control)
        {
            // Initialize subviewmodels
            this.DownloadWidgetVM = new DownloadWidgetViewModel(this.GetParentWindow(), this);

            // Add installed games to list
            this.InstalledGames = new ReactiveList<AppData.InstalledGame.T>(this.AppData.installedGames);

            // Initialize commands
            OpenInstallWindowCommand = ReactiveCommand.Create(OpenInstallWindow);
            StartGameCommand = ReactiveCommand.Create<string>(StartGame);
            UpgradeAllGamesCommand = ReactiveCommand.Create(DownloadWidgetVM.UpgradeAllGames);
        }

        private void OpenInstallWindow()
        {
            var installWindow = new InstallWindow();
            installWindow.DataContext = new InstallWindowViewModel(installWindow, this);
            installWindow.ShowDialog(this.GetParentWindow());
        }

        private static void StartGame(string path)
        {
            if (Core.FSharp.Helpers.os.IsLinux)
            {
                var filepath = System.IO.Path.Combine(path, "start.sh");
                Syscall.chmod(filepath, FilePermissions.ALLPERMS);
                Process.Start(filepath);
            }
            else if (Core.FSharp.Helpers.os.IsWindows)
            {
                // TBD:
            }
            else if (Core.FSharp.Helpers.os.IsMacOS)
            {
                // TBD:
            }
        }
    }
}
