# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2024-03-08

### Changed

- Updated runtime: .NET 6 -> .NET 8
- Updated dependencies

## [0.6.1] - 2022-06-09

### Fixed

-   Some issue when the checksum response isn't valid

## [0.6.0] - 2022-06-07

### Added

-   Installer checksum verification
-   Tabs for process output
-   Icon for non-updateable games
-   AppStream metadata to AppImage

### Changed

-   Show download progress now on top of game tile
-   Updated dependencies

### Fixed

-   Disabled "Start" and "Update" on game which is updating
-   Don't show "Update" option and update notification for non-updateable games

## [0.5.2] - 2022-03-03

### Fixed

-   Show correct version in bottom left

## [0.5.1] - 2022-03-03

### Changed

-   Add delivery as AppImage again
-   Write version to assembly (0 for dev builds)
-   Read version in bottom left corner from assembly

## [0.5.0] - 2022-03-03

### Added
-   "Open game folder" context menu entry
-   Show available dlcs on install

### Changed
-   Moved settings from own window into main one
-   Internal restructurings
-   Uses now .NET 6
-   Application packed with compression (< 40 MB now 🎉)

## [0.4.0] - 2020-12-05

### Added
-   Add ability to choose a game from the list of found games (Install game)
-   Show logos in gamelist (download in UI thread for now)
-   Setting to automatically remove cached installers after 30 days

### Changed
-   Move authentication from own window into main window
-   Move gamelist from left bar to main window
-   Provide default game path to avoid another open window on first opening
-   Move data from LiteDB into json files
-   Update Avalonia.FuncUI

### Fixed
-   Versioning of cached installers now works correctly
-   Use of ENTER in authentication and game search possible
-   After adding games now the right one should start
-   Errors now correctly show up in the error log

## [0.3.0] - 2020-02-29

-   a first very basic GUI (for now without browser with workaround buttons) which can install games, update games and start (some) games
-   supports basic notifications
-   caches installers and reuses already downloaded files
-   uses "unzip" on linux systems for some installers, which otherwise wouldn't install (e.g. State of Mind)
-   internal code restructuring
-   improve handling of aborted downloads
-   settings window where gamepath can be provided
-   terminal output of games in Andromeda

## [0.2.0] - 2018-08-25

-   add automatic downloading and installing of games
-   add ability to automatically upgrade installed games

## [0.1.0] - 2018-07-16

initial, very basic version with console interface

[Unreleased]: https://github.com/NicoVIII/Basealize/compare/v0.6.0...HEAD
[0.6.0]: https://github.com/NicoVIII/Basealize/compare/v0.5.2..v0.6.0
[0.5.2]: https://github.com/NicoVIII/Basealize/compare/v0.5.1..v0.5.2
[0.5.1]: https://github.com/NicoVIII/Basealize/compare/v0.5.0..v0.5.1
[0.5.0]: https://github.com/NicoVIII/Basealize/compare/v0.4.0..v0.5.0
[0.4.0]: https://github.com/NicoVIII/Basealize/compare/v0.3.0..v0.4.0
[0.3.0]: https://github.com/NicoVIII/Basealize/compare/v0.2.0..v0.3.0
[0.2.0]: https://github.com/NicoVIII/Basealize/compare/v0.1.0..v0.2.0
[0.1.0]: https://github.com/NicoVIII/Basealize/releases/v0.1.0
