namespace DiagToolsValidator.Core.CoreFunctionality

open System
open System.Text
open System.IO
open System.Threading
open System.IO.Compression
open System.Formats.Tar
open System.Collections


module Core =
    type ProgressTraceBuilder(loggerPath: string) =
        let _loggerPath = loggerPath

        member this.AppendLineToLogger (line: string) =
            File.AppendAllText(_loggerPath, $"{line}\n")

        member this.Bind(x: Choice<'a, exn>, f: 'a -> Choice<'b, exn>) =
            match x with 
            | Choice2Of2 (ex: exn) -> Choice2Of2 ex
            | Choice1Of2 arg -> 
                let result = f arg
                match result with
                | Choice2Of2 (ex: exn) -> 
                    let errorMessage = new StringBuilder(ex.Message)
                    for entry in ex.Data |> Seq.cast<DictionaryEntry> do
                        errorMessage.AppendLine($"  {entry.Key.ToString()}: {entry.Value.ToString()}") |> ignore
                        errorMessage.AppendLine("Stack Trace") |> ignore
                        errorMessage.AppendLine(ex.StackTrace) |> ignore
                        errorMessage.AppendLine("Inner Exception") |> ignore
                        errorMessage.AppendLine(ex.InnerException.ToString()) |> ignore
                    printfn "%s" (errorMessage.ToString())

                    if not (isNull(_loggerPath))
                        then this.AppendLineToLogger (errorMessage.ToString())
                    
                | _ -> ignore()
                result

        member this.Return(x) =
            Choice1Of2 x

        member this.ReturnFrom(x) =
            x
            
        member this.YieldFrom(x: Choice<CommandLineTool.CommandInvoker, exn>) =
            if not (isNull(_loggerPath))
            then
                match x with
                | Choice2Of2 ex ->
                    this.AppendLineToLogger (ex.Message)
                    for entry in ex.Data |> Seq.cast<DictionaryEntry> do
                        this.AppendLineToLogger $"  {entry.Key.ToString()}: {entry.Value.ToString()}"
                    this.AppendLineToLogger "Stack Trace:"
                    this.AppendLineToLogger (ex.StackTrace)
                    this.AppendLineToLogger "Inner Exception:"
                    this.AppendLineToLogger (ex.InnerException.ToString())
                | Choice1Of2 res ->
                    this.AppendLineToLogger $"Run command: {res.Command}"
                    this.AppendLineToLogger (res.StandardOutput.ToString())
                    this.AppendLineToLogger (res.StandardError.ToString())
            x

        member this.YieldFrom(x: Choice<'a, exn>) =
            if not (isNull(_loggerPath))
            then    
                match x with
                | Choice2Of2 ex ->
                    this.AppendLineToLogger (ex.Message)
                    for entry in ex.Data |> Seq.cast<DictionaryEntry> do
                        this.AppendLineToLogger $"  {entry.Key.ToString()}: {entry.Value.ToString()}"
                    this.AppendLineToLogger "Stack Trace:"
                    this.AppendLineToLogger (ex.StackTrace)
                    this.AppendLineToLogger "Inner Exception:"
                    this.AppendLineToLogger (ex.InnerException.ToString())
                | Choice1Of2 _ -> ignore()
            x
            
        member this.Combine(a: Choice<CommandLineTool.CommandInvoker, exn>, b: unit -> Choice<CommandLineTool.CommandInvoker, exn>) =
            match a with
            | Choice2Of2 _ -> a
            | Choice1Of2 invoker ->
                if String.IsNullOrEmpty(invoker.StandardError.ToString())
                then 
                    b()
                else 
                    if not invoker.Proc.HasExited
                    then 
                        CommandLineTool.TerminateCommandInvoker(invoker) |> ignore
                    a

        member this.Combine(a: Choice<'a, exn>, b: unit -> Choice<'a, exn>) =
            match a with
            | Choice2Of2 _ -> a
            | Choice1Of2 _ -> b()
            
        member this.Zero() =
            Choice2Of2 (new exn("Empty"))
            
        member this.Delay(funToDelay) = 
            funToDelay

        member this.Run(funToDelay) = 
            funToDelay()

        member this.For(collection, f: 'a -> Choice<'b, exn>) =
            let resultList =
                collection
                |> Seq.map (fun item -> f item)
                |> List.ofSeq

            let failedList =
                resultList
                |> List.filter (
                    fun x ->
                        match x with
                        | Choice1Of2 _ -> false
                        | Choice2Of2 _ -> true)

            if failedList.Length = 0
            then List.last resultList
            else List.last failedList


    let CreateDirectory (path: string) =
        try
            Choice1Of2 (Directory.CreateDirectory path)
        with ex -> 
            ex.Data.Add("CreateDirectory", $"Fail to create directory {path}")
            Choice2Of2 ex


    let DecompressGzippedTar (gzipPath: string) (destinationFolder: string) =
        try
            let tarPath = Path.GetTempFileName()
            (
                use originalFileStream = File.OpenRead gzipPath
                (
                    use decompressedFileStream = File.Create tarPath
                    (
                        use decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress)
                        (
                            decompressionStream.CopyTo(decompressedFileStream)
                        )
                    )
                )
            )
            CreateDirectory destinationFolder |> ignore
            TarFile.ExtractToDirectory(tarPath, destinationFolder, true)
            File.Delete tarPath
            Choice1Of2 (DirectoryInfo(destinationFolder))
        with ex -> 
            ex.Data.Add("DecompressGzippedTar", $"Fail to decompress gzipped tar {gzipPath} to {destinationFolder}")
            Choice2Of2 ex


    let DecompressZip (zipPath: string) (destinationFolder: string) =
        try
            ZipFile.ExtractToDirectory(zipPath, destinationFolder, true)
            Choice1Of2 (DirectoryInfo(destinationFolder))
        with ex -> 
            ex.Data.Add("DecompressZip", $"Fail to decompress gzip file {zipPath} to {destinationFolder}")
            Choice2Of2 ex


    let WaitUntil (p: unit -> bool) =
        while not (p()) do
            Thread.Sleep(1000)