using Andromeda.Core.FSharp;
using Avalonia.Controls;
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

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Windows;

namespace Andromeda.AvaloniaApp.ViewModels.Widgets {
    public class DownloadWidgetViewModel : SubViewModelBase {
        private readonly Queue<InstallationInfos> downloadQueue = new Queue<InstallationInfos>();

        private readonly IReactiveList<DownloadStatus> downloads = new ReactiveList<DownloadStatus>();
        public IReactiveList<DownloadStatus> Downloads { get => this.downloads; }

        public DownloadWidgetViewModel(Control control, ViewModelBase parent) : base(control, parent) {
            // Search regularly for upgrades
            this.UpgradeAllGames();
            var timer = new Timer(1 * 3600 * 1000);
            timer.AutoReset = true;
            timer.Elapsed += (a, b) => this.UpgradeAllGames();
            timer.Start();
        }

        // Overwrite AppData Setter to ensure that InstalledGames are always up-to-date
        protected override void SetAppData(DomainTypes.AppData appData) {
            base.SetAppData(appData);
            if (((MainWindowViewModel)this.Parent).InstalledGames != null) {
                ((MainWindowViewModel)this.Parent).InstalledGames.Clear();
                ((MainWindowViewModel)this.Parent).InstalledGames.AddRange(appData.installedGames);
            }
        }

        public void UpgradeAllGames() {
            if (this.AppData.authentication.IsAuth) {
                this.SetAppData(Installed.searchInstalled(this.AppData));
                var result = Installed.checkAllForUpdates(this.AppData);
                this.SetAppData(result.Item2);

                var list = result.Item1.ToList();
                var message = "Found " + list.Count() + " games to update.";
                Logger.LogInfo(message);
                ((MainWindowViewModel)this.Parent).AddNotification(message);
                foreach (var updateInfo in list) {
                    var game = this.AppData.installedGames.Where(g => g.id == updateInfo.game.id).FirstOrDefault();
                    Debug.Assert(game != null);
                    if (updateInfo.newVersion != game.version) // Just to be sure
                    {
                        var result2 = Games.getAvailableInstallersForOs(this.AppData, game.id);
                        this.SetAppData(result2.Item2);

                        var installerInfo = result2.Item1.ToList().First();
                        AddDownload(new InstallationInfos(game.name, installerInfo));
                    }
                }
            }
        }

        public void AddDownload(InstallationInfos info) {
            this.downloadQueue.Enqueue(info);
            Logger.LogInfo("Added download of " + info.GameTitle + " to download queue.");

            this.CheckForNewDownload();
        }

        private void CheckForNewDownload() {
            // TODO: Check for nr of already running downloads
            if (this.downloadQueue.Count > 0) {
                this.StartDownload(this.downloadQueue.Dequeue());
            }
        }

        private void StartDownload(InstallationInfos info) {
            Logger.LogInfo("Get download info for " + info.GameTitle + " to download queue.");
            var res = Games.downloadGame(this.AppData, info.GameTitle, info.InstallerInfo);
            // TODO: This is installation stuff, this should be moved to core
            if (res != null) {
                var filepath = res.Value.Item2;
                var tmppath = res.Value.Item3;
                var downloadInfo = new DownloadStatus(info.GameTitle, tmppath, res.Value.Item4 / 1000000.0f);
                this.Downloads.Add(downloadInfo);

                Timer timer = null;
                Task downloadTask = null;

                // File not found in cache
                if (res.Value.Item1 != null) {
                    Logger.LogInfo("Download installer for " + info.GameTitle + ".");
                    downloadTask = res.Value.Item1.Value;
                    timer = new Timer(500.0);
                    timer.AutoReset = true;
                    timer.Elapsed += (a, b) =>
                    {
                        var fileInfo = new FileInfo(downloadInfo.FilePath);
                        downloadInfo.UpdateDownloaded(fileInfo.Length / 1000000.0f);
                    };
                    timer.Start();
                }
                else {
                    Logger.LogInfo("Use cached installer for " + info.GameTitle + ".");
                }

                var worker = new BackgroundWorker();
                worker.DoWork += (arg, arg2) =>
                {
                    if (downloadTask != null) {
                        downloadTask.Wait();
                        File.Move(tmppath, filepath);
                    }
                    downloadInfo.FilePath = filepath;

                    if (timer != null) {
                        timer.Stop();
                    }

                    // Install game
                    downloadInfo.IndicateInstalling();
                    if (downloadInfo.FilePath != null) {
                        Logger.LogInfo("Unpack " + downloadInfo.GameTitle + " from " + downloadInfo.FilePath);
                        Games.extractLibrary(this.AppData, downloadInfo.GameTitle, downloadInfo.FilePath);
                        Logger.LogInfo(downloadInfo.GameTitle + " unpacked successfully!");
                    }
                    else {
                        Logger.LogError("Filepath to installer is empty! Something went wrong...");
                    }
                };
                worker.RunWorkerCompleted += (arg, arg2) =>
                {
                    this.SetAppData(Installed.searchInstalled(this.AppData));
                    this.Downloads.Remove(downloadInfo);
                    Logger.LogInfo("Cleaned up after install.");
                };
                worker.RunWorkerAsync();
            }
        }
    }
}
