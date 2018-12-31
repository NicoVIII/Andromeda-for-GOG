using Andromeda.Core.FSharp;
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

namespace Andromeda.AvaloniaApp.ViewModels.Widgets
{
    public class DownloadWidgetViewModel : SubViewModelBase
    {
        private readonly Queue<InstallationInfos> downloadQueue = new Queue<InstallationInfos>();

        public readonly IReactiveList<DownloadStatus> Downloads = new ReactiveList<DownloadStatus>();

        public DownloadWidgetViewModel(ViewModelBase parent) : base(parent)
        {
            // Search regularly for upgrades
            this.UpgradeAllGames();
            var timer = new Timer(1 * 3600 * 1000);
            timer.AutoReset = true;
            timer.Elapsed += (a, b) => this.UpgradeAllGames();
            timer.Start();
        }

        public void UpgradeAllGames()
        {
            var result = Installed.checkAllForUpdates(this.AppData);
            this.AppData = result.Item2;

            var list = result.Item1.ToList();
            Logger.LogInfo("Found " + list.Count() + " games to update.");
            foreach (var updateInfo in list)
            {
                var game = this.AppData.installedGames.Where(g => g.id == updateInfo.game.id).FirstOrDefault();
                Debug.Assert(game != null);
                if (updateInfo.newVersion != game.version)
                { // Just to be sure
                    var result2 = Games.getAvailableInstallersForOs(this.AppData, game.id);
                    this.AppData = result2.Item2;

                    var installerInfo = result2.Item1.ToList().First();
                    AddDownload(new InstallationInfos(game.name, installerInfo));
                }
            }
        }

        public void AddDownload(InstallationInfos info)
        {
            this.downloadQueue.Enqueue(info);
            Logger.LogInfo("Added download of " + info.GameTitle + " to download queue.");

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
            Logger.LogInfo("Get download info for " + info.GameTitle + " to download queue.");
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
                else
                {
                    Logger.LogInfo("Use cached installer for " + info.GameTitle + ".");
                }

                var worker = new BackgroundWorker();
                worker.DoWork += (arg, arg2) =>
                {
                    if (downloadTask != null) {
                        downloadTask.Wait();
                    }

                    if (timer != null) {
                        timer.Stop();
                    }

                    // Install game
                    downloadInfo.IndicateInstalling();
                    if (downloadInfo.FilePath != null)
                    {
                        Logger.LogInfo("Unpack " + downloadInfo.GameTitle + " from " + downloadInfo.FilePath);
                        Games.extractLibrary(downloadInfo.GameTitle, downloadInfo.FilePath);
                        Logger.LogInfo(downloadInfo.GameTitle + " unpacked successfully!");
                    }
                    else
                    {
                        Logger.LogError("Filepath to installer is empty! Something went wrong...");
                    }
                };
                worker.RunWorkerCompleted += (arg, arg2) =>
                {
                    this.AppData = Installed.searchInstalled(this.AppData);
                    this.Downloads.Remove(downloadInfo);
                    Logger.LogInfo("Cleaned up after install.");
                };
                worker.RunWorkerAsync();
            }
        }
    }
}
