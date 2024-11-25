namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Collections.Generic
open System.Net.Http

open Core
open DotNet


module DotNetTool =
    let GetToolDll (toolRoot: string) (toolName: string) (toolVersion: string) =
        let intermediateDirectoryRoot = Path.Combine(toolRoot, ".store", toolName, toolVersion, toolName, toolVersion, "tools")
        try
            let intermediateDirectory = 
                Directory.GetDirectories(intermediateDirectoryRoot, "net*")
                |> Array.head
            let toolDll = Path.Combine(intermediateDirectory, "any", $"{toolName}.dll")
            Choice1Of2 toolDll
        with ex -> Choice2Of2 (new exn($"GetToolDll: Fail to find dll for {toolName} in {toolRoot}: {ex.Message}"))


    let InstallDotNetTool (env: Dictionary<string, string>)
                          (dotNetRoot: string)
                          (toolRoot: string)
                          (toolFeed: string)
                          (toolVersion: string) 
                          (toolName: string) =
        RunDotNetCommand 
            dotNetRoot 
            $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}"
            ""
            env
            true
            false


    let DownloadPerfcollect (perfcollectPath: string) =
        let perfcollectUrl = "https://raw.githubusercontent.com/microsoft/perfview/main/src/perfcollect/perfcollect"
        task {
            use httpClient = new HttpClient()
            let! response = httpClient.GetAsync(perfcollectUrl)
            response.EnsureSuccessStatusCode() |> ignore
            let! streamReader = response.Content.ReadAsStreamAsync()
            let streamWriter = File.OpenWrite(perfcollectPath)
            
            streamReader.CopyToAsync(streamWriter) |> ignore
            return perfcollectPath
        }
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.RunSynchronously
