using Andromeda.Core.FSharp;
using Andromeda.AvaloniaApp.Windows;
using Avalonia.Controls;
using Mono.Unix.Native;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public ReactiveCommand<string, Unit> StartGameCommand { get; }

        public MainWindowViewModel() : base()
        {
            this.InstalledGames = new ReactiveList<AppData.InstalledGame.T>(this.AppData.installedGames.ToList());

            // Initialize commands
            OpenInstallWindowCommand = ReactiveCommand.Create(OpenInstallWindow);
            StartGameCommand = ReactiveCommand.Create<string>(StartGame);
        }

        void OpenInstallWindow()
        {
            var installWindow = new InstallWindow();
            installWindow.DataContext = new InstallWindowViewModel(this.AppDataWrapper);
            installWindow.ShowDialog();
        }

        void StartGame(string path)
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
