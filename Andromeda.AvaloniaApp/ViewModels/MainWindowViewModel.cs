using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;

using Andromeda.Core.FSharp;
using Mono.Unix.Native;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Timers;

using static Andromeda.Core.FSharp.Games;
using static Andromeda.Core.FSharp.Installed;

namespace Andromeda.AvaloniaApp.ViewModels
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
            private set { this.RaiseAndSetIfChanged(ref this.installedGames, value); }
        }

        private IReactiveList<DownloadStatus> downloads;
        public IReactiveList<DownloadStatus> Downloads
        {
            get => this.downloads;
            private set { this.RaiseAndSetIfChanged(ref this.downloads, value); }
        }

        private Queue<InstallationInfos> downloadQueue;

        public ReactiveCommand<Unit, Unit> OpenInstallWindowCommand { get; }
        public ReactiveCommand<string, Unit> StartGameCommand { get; }

        public MainWindowViewModel() : base()
        {
            this.InstalledGames = new ReactiveList<AppData.InstalledGame.T>(this.AppData.installedGames.ToList());
            this.Downloads = new ReactiveList<DownloadStatus>();
            this.downloadQueue = new Queue<InstallationInfos>();

            // Initialize commands
            OpenInstallWindowCommand = ReactiveCommand.Create(OpenInstallWindow);
            StartGameCommand = ReactiveCommand.Create<string>(StartGame);
        }

        private void OpenInstallWindow()
        {
            var installWindow = new InstallWindow();
            installWindow.DataContext = new InstallWindowViewModel(this);
            installWindow.ShowDialog();
        }

        public void AddDownload(InstallationInfos info)
        {
            this.downloadQueue.Enqueue(info);
            Console.WriteLine("Added download of " + info.GameTitle + " to download queue.");

            this.CheckForNewDownload();
        }

        private void CheckForNewDownload()
        {
            // TODO: Check for nr of already running downloads
            if (this.downloadQueue.Count > 0)
            {
                this.StartDownload(this.downloadQueue.Dequeue());
            }
        }

        private void StartDownload(InstallationInfos info)
        {
            Console.WriteLine("Get download info for " + info.GameTitle + " to download queue.");
            var res = Games.downloadGame(this.AppData, info.GameTitle, info.InstallerInfo);
            if (res != null)
            {
                var downloadInfo = new DownloadStatus(info.GameTitle, res.Value.Item2, res.Value.Item3 / 1000000.0f);
                this.Downloads.Add(downloadInfo);

                Timer timer = null;
                Task downloadTask = null;

                // File not found in cache
                if (res.Value.Item1 != null)
                {
                    Console.WriteLine("Download installer for " + info.GameTitle + ".");
                    downloadTask = res.Value.Item1.Value;
                    timer = new Timer(1000.0);
                    timer.AutoReset = true;
                    timer.Elapsed += (a, b) =>
                    {
                        var fileInfo = new FileInfo(downloadInfo.FilePath);
                        downloadInfo.UpdateDownloaded(fileInfo.Length / 1000000.0f);
                    };
                    timer.Start();
                }
                else
                {
                    Console.WriteLine("Use cached installer for " + info.GameTitle + ".");
                }

                var worker = new BackgroundWorker();
                worker.DoWork += (arg, arg2) => {
                    if (downloadTask != null)
                        downloadTask.Wait();

                    if (timer != null)
                        timer.Stop();

                    // Install game
                    downloadInfo.IndicateInstalling();
                    if (downloadInfo.FilePath != null)
                    {
                        Console.WriteLine("Unpack " + downloadInfo.GameTitle + " from " + downloadInfo.FilePath);
                        extractLibrary(downloadInfo.GameTitle, downloadInfo.FilePath);
                        Console.WriteLine(downloadInfo.GameTitle + " unpacked successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Filepath to installer is empty! Something went wrong...");
                    }
                };
                worker.RunWorkerCompleted += (arg, arg2) => {
                    this.AppData = searchInstalled(this.AppData);
                    this.Downloads.Remove(downloadInfo);
                    Console.WriteLine("Cleaned up after install.");
                };
                worker.RunWorkerAsync();
            }
        }

        private void StartGame(string path)
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
