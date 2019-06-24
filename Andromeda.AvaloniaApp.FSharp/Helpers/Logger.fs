module Andromeda.AvaloniaApp.FSharp.Helpers.Logger

    open System

    type LogLevel = Info=0 | Warning=1 | Error=2

#if DEBUG
    let logLevel: LogLevel = LogLevel.Info
#else
    let logLevel: LogLevel = LogLevel.Warning
#endif

    let Log level message =
        match level with
        | level when logLevel <= level ->
            Console.WriteLine("[" + level.ToString() + "] " + message)
        | _ -> ()

    let LogError = Log LogLevel.Error
    let LogWarning = Log LogLevel.Warning
    let LogInfo = Log LogLevel.Info
