namespace DiagToolsValidationToolSet.Core.Function

open System.IO
open System.Runtime.InteropServices
open System.Net.Http
open System.Collections.Generic

open DiagToolsValidationToolSet.Core.Utility.Terminal

module DotNetSDKAndRuntime =
    let DownloadInstallScript (scriptPath: string) =
        let scriptUrl =
            if RuntimeInformation.RuntimeIdentifier.Contains "win"
            then "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1"
            else "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh"
        async {
            try
                use httpClient = new HttpClient()
                let! response = httpClient.GetAsync(scriptUrl) |> Async.AwaitTask
                response.EnsureSuccessStatusCode() |> ignore
                let! streamReader = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                let streamWriter = File.OpenWrite(scriptPath)
            
                streamReader.CopyToAsync(streamWriter) |> Async.AwaitTask |> ignore
                return Choice1Of2 scriptPath
            with ex -> return Choice2Of2 (new exn($"DownloadInstallScript: Fail to download install script: {ex.Message}."))
        }

    let MakeScriptRunnable (scriptPath: string) =
        if RuntimeInformation.RuntimeIdentifier.Contains "win"
        then 
            Choice1Of2 scriptPath
        else 
            let envPlaceHolder = new Dictionary<string, string>()
            let result = RunCommandSync envPlaceHolder "chmod" $"+x {scriptPath}"
            match result with
            | Choice1Of2 _ -> Choice1Of2 scriptPath
            | Choice2Of2 ex -> Choice2Of2 ex
            
    let DownloadSDKWithScript (scriptPath: string) (dotnetRoot: string) (dotnetSDKVersion: string) =
        let shellEngine =
            if RuntimeInformation.RuntimeIdentifier.Contains "win"
            then "powershell.exe"
            else "/bin/bash"

        let envPlaceHolder = new Dictionary<string, string>()
        let result = RunCommandSync envPlaceHolder shellEngine $"{scriptPath} -i {dotnetRoot} -v {dotnetSDKVersion}"
        match result with
        | Choice1Of2 _ -> Choice1Of2 dotnetRoot
        | Choice2Of2 ex -> Choice2Of2 ex