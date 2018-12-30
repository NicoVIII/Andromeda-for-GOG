using Andromeda.Core.FSharp;
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
        // Overwrite AppData Setter to ensure that InstalledGames are always up-to-date
        protected override AppData.AppData AppData
        {
            set
            {
                base.AppData = value;
                if (this.InstalledGames != null)
                {
                    this.InstalledGames.Clear();
                    this.InstalledGames.AddRange(value.installedGames.ToList());
                }
            }
        }

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

        public MainWindowViewModel() : base()
        {
            // Initialize subviewmodels
            this.DownloadWidgetVM = new DownloadWidgetViewModel(this);

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
            installWindow.DataContext = new InstallWindowViewModel(this);
            installWindow.ShowDialog();
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
