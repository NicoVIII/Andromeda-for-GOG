using Andromeda.Core.FSharp;
using Avalonia.Controls;
using DynamicData;
using GogApi.DotNet.FSharp;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.ViewModels.Widgets;
using Andromeda.AvaloniaApp.Windows;

namespace Andromeda.AvaloniaApp.ViewModels.Windows
{
    public class InstallWindowViewModel : SubViewModelBase
    {
        private string gameSearchTerm = "";

        public string GameSearchTerm
        {
            get => this.gameSearchTerm;
            private set { this.RaiseAndSetIfChanged(ref this.gameSearchTerm, value); }
        }

        private ISourceList<Listing.ProductInfo> foundGames = new SourceList<Listing.ProductInfo>();

        public ISourceList<Listing.ProductInfo> FoundGames
        {
            get => this.foundGames;
            private set { this.RaiseAndSetIfChanged(ref this.foundGames, value); }
        }

        public ReactiveCommand<Unit, Unit> SearchGameCommand { get; }

        public InstallWindowViewModel(Control control, ViewModelBase parent) : base(control, parent)
        {
            SearchGameCommand = ReactiveCommand.Create(SearchGame);
        }

        void SearchGame()
        {
            var result = Andromeda.Core.FSharp.Games.getAvailableGamesForSearch(this.AppData, GameSearchTerm);
            this.SetAppData(result.Item2);
            if (result.Item1 != null)
            {
                var list = result.Item1.Value.ToList();
                //this.FoundGames.AddRange(list);
                if (list.Count > 0)
                {
                    var game = list.First();
                    var installersTuple = Games.getAvailableInstallersForOs(this.AppData, game.id);
                    this.SetAppData(installersTuple.Item2);
                    var list2 = installersTuple.Item1.ToList();
                    if (list2.Count() > 0)
                    {
                        var installerInfo = list2.First();

                        var downloadWidgetVM = this.Parent.GetChildrenOfType<DownloadWidgetViewModel>().FirstOrDefault();
                        Debug.Assert(downloadWidgetVM != null);
                        downloadWidgetVM.AddDownload(new InstallationInfos(game.title, installerInfo));
                    }
                } else {
                    Logger.LogWarning("Found no matching game for search term!");
                }
            }
        }
    }
}
