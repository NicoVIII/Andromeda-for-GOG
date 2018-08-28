using Andromeda.Core.FSharp;
using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

using static Andromeda.Core.FSharp.Installed;
using static GogApi.DotNet.FSharp.GalaxyApi;
using static GogApi.DotNet.FSharp.Listing;

namespace Andromeda.AvaloniaApp.ViewModels
{
    public class DownloadWindowViewModel : ViewModelBase
    {
        private float downloadedMB;
        public float DownloadedMB
        {
            get => this.downloadedMB;
            private set { this.RaiseAndSetIfChanged(ref this.downloadedMB, value); }
        }

        private float fileMB;
        public float FileMB
        {
            get => this.fileMB;
            private set { this.RaiseAndSetIfChanged(ref this.fileMB, value); }
        }

        public Task DownloadTask { get; private set; }
        public string FilePath { get; private set; }

        public DownloadWindowViewModel(InstallerInfo installerToDownload, string gameTitle, AppDataWrapper appDataWrapper) : base(appDataWrapper)
        {
            var res = Games.downloadGame(this.AppData, gameTitle, installerToDownload);
            if (res != null)
            {
                // File not found in cache
                this.FilePath = res.Value.Item2;
                if (res.Value.Item1 != null) {
                    var task = res.Value.Item1.Value;
                    this.FileMB = res.Value.Item3 / 1000000.0f;
                    var timer = new Timer(1000.0);
                    timer.AutoReset = true;
                    timer.Elapsed += (a, b) =>
                    {
                        var fileInfo = new FileInfo(this.FilePath);
                        this.DownloadedMB = fileInfo.Length / 1000000.0f;
                    };
                    timer.Start();
                    this.DownloadTask = Task.Factory.StartNew(() => {
                        task.Wait();
                        timer.Stop();
                    });
                }
            }
        }
    }
}
