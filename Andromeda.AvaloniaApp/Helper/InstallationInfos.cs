using static GogApi.DotNet.FSharp.GalaxyApi;

namespace Andromeda.AvaloniaApp.Helpers
{
    public class InstallationInfos
    {
        public InstallerInfo InstallerInfo { get; set; }
        public string GameTitle { get; set; }

        public InstallationInfos(string gameTitle, InstallerInfo installerInfo)
        {
            this.GameTitle = gameTitle;
            this.InstallerInfo = installerInfo;
        }
    }
}
