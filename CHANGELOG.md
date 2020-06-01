# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0-alpha.2]

### Added
-   Add ability to choose a game from the list of found games (Install game)
-   Show logos in gamelist (download in UI thread for now)

### Changed
-   Improve subwindow closing behavior
-   Move gamelist from left bar to main window

### Fixed
-   Versioning of cached installers now works correctly

## [0.3.0]

-   a first very basic GUI (for now without browser with workaround buttons) which can install games, update games and start (some) games
-   supports basic notifications
-   caches installers and reuses already downloaded files
-   uses "unzip" on linux systems for some installers, which otherwise wouldn't install (e.g. State of Mind)
-   internal code restructuring
-   improve handling of aborted downloads
-   settings window where gamepath can be provided
-   terminal output of games in Andromeda

## [0.2.0]

-   add automatic downloading and installing of games
-   add ability to automatically upgrade installed games

## [0.1.0]

initial, very basic version with console interface
