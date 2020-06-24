namespace Andromeda.Core.FSharp

open System
open System.IO

[<AutoOpen>]
module Helpers =
    // Taken and converted to F# from https://blez.wordpress.com/2013/02/18/get-file-shortcuts-target-with-c/
    let getShortcutTarget file =
        try
            if Path.GetExtension(file).ToLower().Equals(".lnk")
               |> not then
                Exception("Supplied file must be a .LNK file")
                |> raise

            let fileStream =
                File.Open(file, FileMode.Open, FileAccess.Read)

            use fileReader = new BinaryReader(fileStream)
            fileStream.Seek(int64 (0x14), SeekOrigin.Begin)
            |> ignore // Seek to flags
            let flags = fileReader.ReadUInt32() // Read flags
            if (flags &&& uint32 (1)).Equals(uint32 (1)) then // Bit 1 set means we have to skip the shell item ID list
                fileStream.Seek(int64 (0x4c), SeekOrigin.Begin)
                |> ignore // Seek to the end of the header
                let offset = fileReader.ReadUInt16() // Read the length of the Shell item ID list
                fileStream.Seek(int64 (offset), SeekOrigin.Current)
                |> ignore // Seek past it (to the file locator info)

            let fileInfoStartsAt = fileStream.Position // Store the offset where the file info
            // structure begins
            let totalStructLength = fileReader.ReadUInt32() // read the length of the whole struct
            fileStream.Seek(int64 (0xc), SeekOrigin.Current)
            |> ignore // seek to offset to base pathname
            let fileOffset = fileReader.ReadUInt32() // read offset to base pathname
            // the offset is from the beginning of the file info struct (fileInfoStartsAt)
            fileStream.Seek((fileInfoStartsAt + int64 (fileOffset)), SeekOrigin.Begin)
            |> ignore // Seek to beginning of
            // base pathname (target)
            let pathLength =
                (int64 (totalStructLength) + fileInfoStartsAt)
                - fileStream.Position
                - int64 (2) // read
            // the base pathname. I don't need the 2 terminating nulls.
            let linkTarget = fileReader.ReadChars((int) pathLength) // should be unicode safe
            let link = new string(linkTarget)

            let start = link.IndexOf("\0\0")
            if start > -1 then
                let mutable ending = link.IndexOf("\\\\", start + 2) + 2
                ending <- link.IndexOf(char ("\0"), ending) + 1

                let firstPart = link.Substring(0, start)
                let secondPart = link.Substring(ending)

                firstPart + secondPart
            else
                link
        with _ -> ""

    type ResultBuilder() =
        member this.Return x = Ok x
        member this.Zero() = Ok()
        member this.Bind(xResult, f) = Result.bind f xResult

    let result = ResultBuilder()

    let removeInvalidFileNameChars part =
        Path.GetInvalidFileNameChars()
        |> Seq.fold (fun name c -> String.replace (string c) "" name) part
