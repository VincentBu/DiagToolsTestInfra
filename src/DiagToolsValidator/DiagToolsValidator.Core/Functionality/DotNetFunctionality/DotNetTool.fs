namespace DiagToolsValidator.Core.Functionality

open System.IO
open System.Net.Http
open System.Collections.Generic


module DotNetTool =
    let GetToolIL (toolRoot: string) (toolName: string) (toolVersion: string) =
        let intermediateDirectoryRoot = Path.Combine(toolRoot, ".store", toolName, toolVersion, toolName, toolVersion, "tools")
        try
            let intermediateDirectory = 
                Directory.GetDirectories(intermediateDirectoryRoot, "net*")
                |> Array.head
            Choice1Of2 (Path.Combine(intermediateDirectory, "any", $"{toolName}.dll"))
        with ex -> 
            ex.Data.Add("DotNetTool.GetToolDll", $"Fail to find IL for {toolName} in {toolRoot}")
            Choice2Of2 ex


    let InstallDotNetTool (dotNetEnv: Dictionary<string, string>)
                          (toolRoot: string)
                          (toolFeed: string)
                          (configFilePath: string) 
                          (toolVersion: string) 
                          (toolName: string) =
        let argument = 
            if Path.Exists(configFilePath)
            then $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed} --configfile {configFilePath}"
            else $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}"
        DotNet.RunDotNetCommand dotNetEnv 
                                argument
                                ""
                                true
                                CommandLineTool.PrinteOutputData
                                CommandLineTool.PrintErrorData
                                true


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
        
