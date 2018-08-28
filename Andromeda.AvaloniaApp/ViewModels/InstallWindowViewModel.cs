using Andromeda.Core.FSharp;
using Andromeda.AvaloniaApp.Helpers;
using Andromeda.AvaloniaApp.Windows;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

using static Andromeda.Core.FSharp.Games;
using static Andromeda.Core.FSharp.Installed;
using static GogApi.DotNet.FSharp.Listing;

namespace Andromeda.AvaloniaApp.ViewModels
{
    public class InstallWindowViewModel : ViewModelBase
    {
        private string gameSearchTerm = "";

        public string GameSearchTerm
        {
            get => this.gameSearchTerm;
            private set { this.RaiseAndSetIfChanged(ref this.gameSearchTerm, value); }
        }

        private ISourceList<ProductInfo> foundGames = new SourceList<ProductInfo>();

        public ISourceList<ProductInfo> FoundGames
        {
            get => this.foundGames;
            private set { this.RaiseAndSetIfChanged(ref this.foundGames, value); }
        }

        public ReactiveCommand<Unit, Unit> SearchGameCommand { get; }

        public InstallWindowViewModel(AppDataWrapper appDataWrapper) : base(appDataWrapper)
        {
            SearchGameCommand = ReactiveCommand.Create(SearchGame);
        }

        void SearchGame()
        {
            var result = Andromeda.Core.FSharp.Games.getAvailableGamesForSearch(this.AppData, GameSearchTerm);
            this.AppData = result.Item2;
            if (result.Item1 != null)
            {
                var list = result.Item1.Value.ToList();
                //this.FoundGames.AddRange(list);
                if (list.Count > 0)
                {
                    var game = list.First();
                    var installersTuple = getAvailableInstallersForOs(this.AppData, game.id);
                    this.AppData = installersTuple.Item2;
                    var list2 = installersTuple.Item1.ToList();
                    if (list2.Count() > 0)
                    {
                        var installerInfo = list2.First();

                        var downloadWindow = new DownloadWindow();
                        var downloadViewModel = new DownloadWindowViewModel(installerInfo, game.title, this.AppDataWrapper);
                        downloadWindow.DataContext = downloadViewModel;
                        if (downloadViewModel.DownloadTask != null)
                        {
                            downloadWindow.Show();
                        }
                        Task.Factory.StartNew(() =>
                        {
                            if (downloadViewModel.DownloadTask != null)
                            {
                                downloadViewModel.DownloadTask.Wait();
                            }

                            if (downloadViewModel.FilePath != null) {
                                Console.WriteLine("Unpack " + game.title + " from " + downloadViewModel.FilePath);
                                extractLibrary(game.title, downloadViewModel.FilePath);
                                Console.WriteLine(game.title + " unpacked successfully!");
                            } else {
                                Console.WriteLine("Filepath to installer is empty! Something went wrong...");
                            }
                            this.AppData = searchInstalled(this.AppData);
                        });
                    }
                }
            }
        }
    }
}
