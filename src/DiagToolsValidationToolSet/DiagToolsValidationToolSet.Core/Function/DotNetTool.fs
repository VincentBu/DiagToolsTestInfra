namespace DiagToolsValidationToolSet.Core.Function

open System.IO
open System.Collections.Generic

open DiagToolsValidationToolSet.Core.Utility
open System.Net.Http

module DotNetTool =
    let InstallDotNetTool (env: Dictionary<string, string>)
                          (dotnetBinPath: string)
                          (toolRoot: string)
                          (toolFeed: string)
                          (toolName: string)
                          (toolVersion: string) =
        let result = Terminal.RunCommandSync env "" dotnetBinPath $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}"
        match result with
        | Choice1Of2 _ -> Choice1Of2 toolRoot
        | Choice2Of2 ex -> Choice2Of2 ex


    let DownloadPerfcollect (perfcollectPath: string) =
        let perfcollectUrl = "https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect"
        async {
            try
                use httpClient = new HttpClient()
                let! response = httpClient.GetAsync(perfcollectUrl) |> Async.AwaitTask
                response.EnsureSuccessStatusCode() |> ignore
                let! streamReader = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                let streamWriter = File.OpenWrite(perfcollectPath)
            
                streamReader.CopyToAsync(streamWriter) |> Async.AwaitTask |> ignore
                return Choice1Of2 perfcollectPath
            with ex -> return Choice2Of2 (new exn($"DownloadPerfcollect: Fail to download perfcollect: {ex.Message}."))
        }


    let GetToolDll (toolRoot: string) (toolName: string) (toolVersion: string) =
        let intermediateDirectoryRoot = Path.Combine(toolRoot, ".store", toolName, toolVersion, toolName, toolVersion, "tools")
        try
            let intermediateDirectory = 
                Directory.GetDirectories(intermediateDirectoryRoot, "net*")
                |> Array.head
            let toolDll = Path.Combine(intermediateDirectory, "any", $"{toolName}.dll")
            Choice1Of2 toolDll
        with ex -> Choice2Of2 (new exn($"GetToolDll: Fail to find dll for {toolName} in {toolRoot}: {ex.Message}"))