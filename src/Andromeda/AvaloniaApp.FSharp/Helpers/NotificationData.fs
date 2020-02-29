namespace Andromeda.AvaloniaApp.FSharp.Helpers

type NotificationData (message: string) =
    member val Message = message with get,set
