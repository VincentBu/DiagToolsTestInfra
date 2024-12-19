namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Net.Http


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


    let InstallDotNetTool (dotNetRoot: string)
                          (toolRoot: string)
                          (toolFeed: string)
                          (toolVersion: string) 
                          (toolName: string) =
        DotNet.RunDotNetCommand dotNetRoot 
                                $"tool install {toolName} --tool-path {toolRoot} --version {toolVersion} --add-source {toolFeed}"
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
        
