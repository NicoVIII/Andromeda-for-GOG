using Andromeda.Core.FSharp;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using Mono.Unix.Native;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Widgets;
using Andromeda.AvaloniaApp.Windows;

namespace Andromeda.AvaloniaApp.ViewModels.Windows
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Version { get => "v0.3.0-alpha.3"; }

        private IReactiveList<AppData.InstalledGame.T> installedGames;
        public IReactiveList<AppData.InstalledGame.T> InstalledGames
        {
            get => this.installedGames;
            set => this.RaiseAndSetIfChanged(ref this.installedGames, value);
        }

        private readonly IReactiveList<NotificationData> notifications = new ReactiveList<NotificationData>();
        public IReactiveList<NotificationData> Notifications { get => this.notifications; }

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
            // Add installed games to list
            this.InstalledGames = new ReactiveList<AppData.InstalledGame.T>(this.AppData.installedGames);

            // Initialize subviewmodels
            this.DownloadWidgetVM = new DownloadWidgetViewModel(this.GetParentWindow(), this);

            // Initialize commands
            OpenInstallWindowCommand = ReactiveCommand.Create(OpenInstallWindow);
            StartGameCommand = ReactiveCommand.Create<string>(StartGame);
            UpgradeAllGamesCommand = ReactiveCommand.Create(DownloadWidgetVM.UpgradeAllGames);
        }

        public void AddNotification(string message)
        {
            var notification = new NotificationData(message);
            this.Notifications.Add(notification);

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            var timer = new System.Timers.Timer(5000.0);
            timer.Elapsed += (a, b) =>
            {
                Task.Factory.StartNew(
                    () => this.Notifications.Remove(notification),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler
                );
            };
            timer.Start();
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
