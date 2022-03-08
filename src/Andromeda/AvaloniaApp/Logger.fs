namespace Andromeda.AvaloniaApp

open System

[<AutoOpen>]
module Logger =
    type LogLevel =
        | Info = 0
        | Warning = 1
        | Error = 2

#if DEBUG
    let logLevel: LogLevel = LogLevel.Info
#else
    let logLevel: LogLevel = LogLevel.Warning
#endif

    let log level message =
        match level with
        | level when logLevel <= level ->
            Console.WriteLine("[" + level.ToString() + "] " + message)
        | _ -> ()

    let logError = log LogLevel.Error
    let logWarning = log LogLevel.Warning
    let logInfo = log LogLevel.Info
