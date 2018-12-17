using static GogApi.DotNet.FSharp.GalaxyApi;

namespace Andromeda.AvaloniaApp.Helpers
{
    public class DownloadStatus
    {
        public string GameTitle { get; private set; }
        public string FilePath { get; private set; }
        public float FileSize { get; private set; }
        public float Downloaded { get; private set; }
        public bool Installing { get; private set; }

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
