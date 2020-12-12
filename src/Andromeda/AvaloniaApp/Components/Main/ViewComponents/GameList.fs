namespace Andromeda.AvaloniaApp.Components.Main.ViewComponents

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Lenses
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Media
open Avalonia.Media.Imaging

open Andromeda.AvaloniaApp.AvaloniaHelper
open Andromeda.AvaloniaApp.Components.Main

module GameList =
    let gameTile dispatch (i: int, game: InstalledGame): IView =
        let gap = 5.0

        Border.create [
            Border.margin (0.0, 0.0, gap, gap)
            Border.contextMenu
                (ContextMenu.create [
                    ContextMenu.viewItems [
                        MenuItem.create [
                            MenuItem.header "Start"
                            MenuItem.onClick
                                ((fun _ -> game |> StartGame |> dispatch),
                                 OnChangeOf game)
                        ]
                        MenuItem.create [
                            MenuItem.header "Update"
                            MenuItem.onClick
                                ((fun _ -> game |> UpgradeGame |> dispatch),
                                 OnChangeOf game)
                        ]
                        MenuItem.create [ MenuItem.header "-" ]
                        MenuItem.create [
                            MenuItem.header "Open game folder"
                            MenuItem.onClick
                                ((fun _ -> game.path |> System.openFolder),
                                 OnChangeOf game)
                        ]
                    ]
                 ])
            Border.child
                (Image.create [
                    Image.height 120.0
                    Image.stretch Stretch.UniformToFill
                    Image.width 200.0
                    Image.source
                        (match game.image with
                         | Some imgPath -> new Bitmap(imgPath)
                         | None ->
                             new Bitmap(loadAssetPath
                                            "avares://Andromeda.AvaloniaApp/Assets/placeholder.jpg"))
                 ])
        ]
        :> IView

    let render state dispatch =
        WrapPanel.create [
            WrapPanel.children
                (state
                 |> getl StateLenses.installedGames
                 |> Map.toList
                 |> List.map snd
                 |> List.indexed
                 |> List.map (gameTile dispatch))
        ]
