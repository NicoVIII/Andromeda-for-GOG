using ReactiveUI;

using static GogApi.DotNet.FSharp.GalaxyApi;

namespace Andromeda.AvaloniaApp.Helpers
{
    public class DownloadStatus : ReactiveObject
    {
        private float downloaded;
        private bool installing;

        public string GameTitle { get; private set; }
        public string FilePath { get; set; }
        public float FileSize { get; private set; }

        public float Downloaded {
            get => this.downloaded;
            private set {
                this.RaiseAndSetIfChanged(ref this.downloaded, value);
            }
        }
        public bool Installing {
            get => installing;
            private set {
                this.RaiseAndSetIfChanged(ref this.installing, value);
            }
        }

        public DownloadStatus(string gameTitle, string path, float fileSize)
        {
            this.GameTitle = gameTitle;
            this.FilePath = path;
            this.FileSize = fileSize;
            this.Downloaded = 0;
            this.Installing = false;
        }

        public void UpdateDownloaded(float downloaded)
        {
            this.Downloaded = downloaded;
        }

        public void IndicateInstalling()
        {
            this.Installing = true;
        }
    }
}
