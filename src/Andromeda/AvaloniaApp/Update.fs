namespace Andromeda.AvaloniaApp

open Avalonia.Threading
open Elmish
open GogApi.DomainTypes
open SimpleOptics
open System
open System.Diagnostics
open System.IO
open System.Reactive.Concurrency
open System.Security.Cryptography
open System.Threading.Tasks

open Andromeda.Core
open Andromeda.Core.DomainTypes
open Andromeda.Core.Installed

open Andromeda.AvaloniaApp.Components
open Andromeda.AvaloniaApp.DomainTypes

module Update =
    module Subs =
        /// Starts a given game in a subprocess and redirects its terminal output
        let startGame gameId (gameDir: string) =
            let createEmptyDisposable () =
                { new IDisposable with
                    member __.Dispose() = () }

            let sub dispatch =
                let showGameOutput _ (outLine: DataReceivedEventArgs) =
                    match outLine.Data with
                    | newLine when String.IsNullOrEmpty newLine -> ()
                    | _ ->
                        let state = outLine.Data

                        let action _ newLine =
                            AddToTerminalOutput(gameId, newLine) |> dispatch
                            createEmptyDisposable ()

                        AvaloniaScheduler.Instance.Schedule(
                            state,
                            new Func<IScheduler, string, IDisposable>(action)
                        )
                        |> ignore

                startGameProcess showGameOutput gameDir |> ignore

            Cmd.ofSub sub

        let registerDownloadTimer
            (task: Task<unit> option)
            tmppath
            (game: Game)
            maxFileSize
            =
            let sub dispatch =
                match task with
                | Some _ ->
                    "Download installer for " + game.name + "."
                    |> logInfo

                    let invoke () =
                        let currentFileSize =
                            if File.Exists tmppath then
                                let fileSize =
                                    tmppath
                                    |> FileInfo
                                    |> fun fileInfo ->
                                        byteLToMiB (fileInfo.Length * 1L<Byte>)

                                UpdateDownloadSize(game.id, fileSize) |> dispatch

                                fileSize
                            else
                                0<MiB>

                        File.Exists tmppath
                        && currentFileSize < maxFileSize

                    DispatcherTimer.Run(Func<bool>(invoke), TimeSpan.FromSeconds 0.5)
                    |> ignore
                | None ->
                    "Use cached installer for " + game.name + "."
                    |> logInfo

                    UpdateDownloadSize(game.id, maxFileSize)
                    |> dispatch

            Cmd.ofSub sub

    /// Contains everything for the update method, which is a little longer
    module Update =
        /// Checks a single game for an upgrade
        let upgradeGame state game showNotification =
            // Only update updateable game
            match game.status with
            | Installed (Some _, _) ->
                let invoke () =
                    (Optic.get MainStateOptic.authentication state, game)
                    ||> checkGameForUpdate

                let cmd =
                    Cmd.OfAsync.perform invoke () (fun (updateData, authentication) ->
                        FinishGameUpgrade(
                            game,
                            showNotification,
                            updateData,
                            authentication
                        ))

                state, cmd
            | _ -> state, Cmd.none

        let finishGameUpgrade
            state
            showNotification
            (game: Game)
            (updateData: UpdateData option)
            authentication
            =
            // Update authentication in state, if it was refreshed
            let state = Optic.set MainStateOptic.authentication authentication state

            // Download updated installers or show notification
            let cmd =
                match updateData with
                | Some updateData ->
                    updateData.game
                    |> Game.toProductInfo
                    |> (fun productInfo ->
                        // TODO: update DLCs
                        (productInfo, [], authentication)
                        |> StartGameDownload)
                    |> Cmd.ofMsg
                | None ->
                    if showNotification then
                        AddNotification $"No new version available for %s{game.name}"
                        |> Cmd.ofMsg
                    else
                        Cmd.none

            state, cmd

        /// Sets the local image path for a game
        let setGameImage state productId imgPath =
            let state =
                state
                |> Optic.map
                    MainStateOptic.games
                    (Map.change productId (function
                        | Some game -> { game with image = imgPath |> Some } |> Some
                        | None -> None))

            state, Cmd.none

        let addNotification state notification =
            // Remove notification again after 5 seconds
            let removeNotification notification =
                async {
                    do! Async.Sleep 5000
                    return notification
                }

            let state =
                state
                |> Optic.map MainStateOptic.notifications (List.append [ notification ])

            let cmd =
                Cmd.OfAsync.perform removeNotification notification RemoveNotification

            state, cmd

        let searchInstalled state =
            let authentication = Optic.get MainStateOptic.authentication state

            let (installedGames, imgJobs) =
                (Optic.get MainStateOptic.settings state, authentication)
                ||> Installed.searchInstalled

            let state = Optic.set MainStateOptic.games installedGames state

            let cmd =
                imgJobs
                |> List.map (fun job ->
                    Cmd.OfAsync.perform job authentication SetGameImage)
                |> Cmd.batch

            state, cmd

        let startGameDownload state (productInfo: ProductInfo) (dlcs: Dlc list) =
            // TODO: download and install DLCs
            let authentication = Optic.get MainStateOptic.authentication state

            let installerInfoList =
                Diverse.getAvailableInstallersForOs productInfo.id authentication
                |> Async.RunSynchronously

            match installerInfoList with
            | [] ->
                let cmd = Cmd.ofMsg (AddNotification "Found no installer for this OS...")

                state, cmd
            | [ installerInfo ] ->
                let result =
                    Download.downloadGame productInfo.title installerInfo authentication
                    |> Async.RunSynchronously

                match result with
                | None -> state, Cmd.none
                | Some (task, filePath, tmppath, size, checksum) ->
                    let fileSize =
                        size
                        |> byteToMiBL
                        |> (fun x -> x / 1L<MiB>)
                        |> int
                        |> (fun x -> x * 1<MiB>)

                    let game =
                        Game.create productInfo.id (productInfo.title)
                        |> Optic.set
                            GameOptic.status
                            (Downloading(0<MiB>, fileSize, filePath))

                    let settings = Optic.get MainStateOptic.settings state

                    let state = Optic.set (MainStateOptic.game game.id) game state

                    let downloadCmd =
                        match task with
                        | Some task ->
                            let invoke () =
                                async {
                                    let! _ = task |> Async.AwaitTask
                                    File.Move(tmppath, filePath)
                                }

                            Cmd.OfAsync.perform invoke () (fun _ ->
                                UnpackGame(
                                    settings,
                                    game,
                                    filePath,
                                    checksum,
                                    installerInfo.version
                                ))
                        | None ->
                            UnpackGame(
                                settings,
                                game,
                                filePath,
                                checksum,
                                installerInfo.version
                            )
                            |> Cmd.ofMsg

                    let cmd =
                        [ Subs.registerDownloadTimer task tmppath game fileSize
                          Cmd.ofMsg (LookupGameImage game.id)
                          downloadCmd ]
                        |> Cmd.batch

                    state, cmd
            | _ ->
                let cmd =
                    Cmd.ofMsg (
                        AddNotification
                            "Found multiple installers, this is not supported yet..."
                    )

                state, cmd

    let performAuthenticated msg state mainWindow =
        let inline changeContext context (contextState, cmd) =
            { state with context = context contextState }, cmd

        match msg with
        | StartGame (gameId, gameDir) -> state, Subs.startGame gameId gameDir
        | UpgradeGame (game, showNotification) ->
            Update.upgradeGame state game showNotification
        | FinishGameUpgrade (game, showNotification, updateData, authentication) ->
            Update.finishGameUpgrade state showNotification game updateData authentication
        | LookupGameImage productId ->
            let cmd =
                match Diverse.getProductImg productId with
                | Diverse.AlreadyDownloaded imgPath ->
                    SetGameImage(productId, imgPath) |> Cmd.ofMsg
                | Diverse.HasToBeDownloaded job ->
                    let authentication = Optic.get MainStateOptic.authentication state
                    Cmd.OfAsync.perform job authentication SetGameImage

            state, cmd
        | SetGameImage (productId, imgPath) -> Update.setGameImage state productId imgPath
        | AddNotification notification -> Update.addNotification state notification
        | RemoveNotification notification ->
            let state =
                Optic.map
                    MainStateOptic.notifications
                    (List.filter (fun n -> n <> notification))
                    state

            state, Cmd.none
        | AddToTerminalOutput (productId, newLine) ->
            // Add new line to the front of the terminal
            let state =
                let tabExists =
                    Optic.get (MainStateOptic.gameOutput productId) state
                    |> Option.isSome

                // If the tab does not already exist, initialize it
                if tabExists then
                    state
                else
                    Optic.set (MainStateOptic.gameOutput productId) [] state
                // Limit output to 1000 lines
                |> Optic.map (MainStateOptic.gameOutput productId) (fun lines ->
                    newLine :: lines.[..999])

            state, Cmd.none
        | SearchInstalled initial ->
            // TODO: Async?
            let state, cmd = Update.searchInstalled state

            let cmd' =
                match initial && state.settings.updateOnStartup with
                | true ->
                    [ cmd; UpgradeGames false |> Cmd.ofMsg ]
                    |> Cmd.batch
                | false -> cmd

            state, cmd'
        | CacheCheck ->
            let cacheCheck () =
                async {
                    do
                        Optic.get MainStateOptic.settings state
                        |> Cache.check
                }

            let cmd =
                Cmd.OfAsync.attempt cacheCheck () (fun _ ->
                    AddNotification "Cachecheck failed!")

            state, cmd
        | SetSettings settings ->
            Persistence.Settings.save settings |> ignore

            let state = Optic.set MainStateOptic.settings settings state

            let cmd =
                [ Cmd.ofMsg (SearchInstalled false)
                  Cmd.ofMsg CacheCheck ]
                |> Cmd.batch

            state, cmd
        | FinishGameDownload (gameId, gameDir, version) ->
            let state =
                Optic.set
                    (MainStateOptic.gameStatus gameId)
                    (Installed(version, gameDir))
                    state

            state, Cmd.none
        | StartGameDownload (productInfo, dlcs, authentication) ->
            // This is triggered by the parent component, authentication could have changed,
            // so we update it
            let state = Optic.set MainStateOptic.authentication authentication state

            Update.startGameDownload state productInfo dlcs
        | UnpackGame (settings, game, filePath, checksum, version) ->
            // Check checksum
            use md5 = MD5.Create()
            use stream = File.OpenRead filePath

            let hash =
                BitConverter
                    .ToString(md5.ComputeHash stream)
                    .Replace("-", "")
                    .ToLowerInvariant()

            if hash <> checksum then
                let state =
                    Optic.set
                        (MainStateOptic.gameStatus game.id)
                        (Errored "Checksum failed!")
                        state

                state, Cmd.none
            else
                let invoke () =
                    Download.extractLibrary settings game.name filePath version

                let cmd =
                    [ Cmd.ofMsg (UpdateDownloadInstalling game.id)
                      Cmd.OfAsync.perform invoke () (fun gameDir ->
                          FinishGameDownload(game.id, gameDir, version)) ]
                    |> Cmd.batch

                state, cmd
        | UpgradeGames showNotifications ->
            let games =
                Optic.get MainStateOptic.games state
                |> Map.toList
                |> List.map snd

            // Download updated installers or show notification
            let cmd =
                games
                |> List.map (fun game -> UpgradeGame(game, showNotifications) |> Cmd.ofMsg)
                |> Cmd.batch

            state, cmd
        | UpdateDownloadSize (gameId, fileSize) ->
            let state =
                state
                |> Optic.map (MainStateOptic.gameStatus gameId) (function
                    | Downloading (_, total, filePath) ->
                        Downloading(fileSize, total, filePath)
                    | _ ->
                        failwith "Got new filesize although we are no longer downloading")

            state, Cmd.none
        | UpdateDownloadInstalling gameId ->
            let state =
                state
                |> Optic.map (MainStateOptic.gameStatus gameId) (function
                    | Downloading (_, _, filePath) -> Installing filePath
                    | _ ->
                        failwith
                            "Tried to switch to installing, although we are not downloading")

            state, Cmd.none
        // Context change
        | ShowInstalled ->
            ((), Cmd.none)
            |> changeContext (fun () -> Context.Installed)
        | ShowInstallGame -> InstallGame.init () |> changeContext InstallGame
        | ShowSettings ->
            Settings.init state.settings
            |> changeContext Settings
        // Child components
        | InstallGameMgs msg ->
            match state.context with
            | InstallGame subState ->
                let installGameState, installGameCmd, intent =
                    InstallGame.update state.authentication msg subState

                let state = { state with context = InstallGame installGameState }

                let intentCmd =
                    match intent with
                    | InstallGame.DoNothing -> Cmd.none
                    | InstallGame.Close (productInfo, dlcs) ->
                        [ StartGameDownload(productInfo, dlcs, state.authentication)
                          |> Cmd.ofMsg
                          Cmd.ofMsg ShowInstalled ]
                        |> Cmd.batch

                let cmd =
                    [ Cmd.map InstallGameMgs installGameCmd
                      intentCmd ]
                    |> Cmd.batch

                state, cmd
            | _ ->
                logError "Got InstallGameMsg although context is not InstallGame."
                state, Cmd.none
        | SettingsMsg msg ->
            match state.context with
            | Settings subState ->
                let settingsState, settingsCmd, intent =
                    Settings.update mainWindow msg subState

                let state = { state with context = Settings settingsState }

                let intentCmd =
                    match intent with
                    | Settings.DoNothing -> Cmd.none
                    | Settings.Save settings ->
                        [ SetSettings settings; ShowInstalled ]
                        |> List.map Cmd.ofMsg
                        |> Cmd.batch

                let cmd =
                    [ Cmd.map SettingsMsg settingsCmd
                      intentCmd ]
                    |> Cmd.batch

                state, cmd
            | _ ->
                logError "Got SettingsMsg although context is not Settings."
                state, Cmd.none

    let performUnauthenticated msg state =
        match msg with
        | Authenticate authentication ->
            let state, programCmd = Init.authenticated authentication

            state, Cmd.map Auth programCmd
        | AuthenticationMsg msg ->
            let state, authCmd, intent = Authentication.update msg state

            let intentCmd =
                match intent with
                | Authentication.DoNothing -> Cmd.none
                | Authentication.Authenticate authentication ->
                    Persistence.Authentication.save authentication
                    |> ignore

                    authentication |> Authenticate |> Cmd.ofMsg

            let cmd =
                [ Cmd.map AuthenticationMsg authCmd
                  intentCmd ]
                |> Cmd.batch

            Unauthenticated state, Cmd.map UnAuth cmd

    let perform msg (state: State) mainWindow =
        match msg, state with
        | Auth msg, Authenticated state ->
            let state, cmd = performAuthenticated msg state mainWindow
            Authenticated state, Cmd.map Auth cmd
        | Auth _, Unauthenticated _ -> failwith "Got unauthenticated msg in auth state"
        | UnAuth msg, Unauthenticated state -> performUnauthenticated msg state
        | UnAuth _, Authenticated _ -> failwith "Got authenticated msg in unauth state"
