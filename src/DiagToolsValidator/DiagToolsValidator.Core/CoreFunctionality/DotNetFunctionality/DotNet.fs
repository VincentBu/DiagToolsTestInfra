namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Net.Http
open System.Runtime.InteropServices
open System.Collections.Generic

open Core
open FileSystemFuntion


module DotNet =
    let CurrentRID = RuntimeInformation.RuntimeIdentifier
    
    let AzureFeedList = [
        "https://dotnetcli.azureedge.net/dotnet";
        "https://dotnetbuilds.azureedge.net/public"
    ]


    let GetExcutableFileExtensionByRID (rid: string) =
        if rid.ToLower().StartsWith("win")
        then ".exe"
        else ""


    let GetCompressionExtensionByRID (rid: string) =
        if rid.ToLower().Contains("win")
        then ".zip"
        else ".tar.gz"


    let GenerateDownloadLink (feed: string) (rid: string) (sdkFullVersion: string) =
        let productVersionQueryUrl = $"{feed}/Sdk/{sdkFullVersion}/sdk-productVersion.txt"

        let SDKExtension = GetCompressionExtensionByRID rid

        use httpClient = new HttpClient()
        task {
            let! response = httpClient.GetAsync(productVersionQueryUrl)
            let! content = response.Content.ReadAsStringAsync()
            let productVersion = content.Replace("\n", "").Replace("\r", "")
            return $"{feed}/Sdk/{sdkFullVersion}/dotnet-sdk-{productVersion}-{rid}{SDKExtension}"
        }
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.RunSynchronously
        

    let DownloadCompressedSDK (downloadLink: string) (downloadPath: string) =
        use httpClient = new HttpClient()
        task {
            let! response = httpClient.GetAsync(downloadLink)
            let! readStream = response.Content.ReadAsStreamAsync()
            let writeStream = File.OpenWrite(downloadPath)
            do! readStream.CopyToAsync(writeStream)
            return downloadPath
        }
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.RunSynchronously
        

    let ExtractToDotNetRoot (sdkArchivePath: string) (dotnetRoot: string) =
        if sdkArchivePath.EndsWith(".tar.gz")
        then
            DecompressGzippedTar sdkArchivePath dotnetRoot
        elif sdkArchivePath.EndsWith(".zip")
        then
            DecompressZip sdkArchivePath dotnetRoot
        else
            let extension = Path.GetExtension sdkArchivePath
            Choice2Of2 (new exn($"unknown extension: {extension}"))


    let InstallDotNetSDKByVersion (rid: string) (sdkFullVersion: string) (dotnetRoot: string) =
        let choice = new ChoiceChainBuilder()

        let SDKExtension = GetCompressionExtensionByRID rid
        let downloadPath = Path.GetRandomFileName() + SDKExtension

        choice {
            let! downloadLink = 
                choice {
                    for feed in AzureFeedList do
                        let! downloadLink = 
                            GenerateDownloadLink feed rid sdkFullVersion 
                        return downloadLink
                }
 
            let! downloadPath = DownloadCompressedSDK downloadLink downloadPath
            
            let! dotnetRootDirInfo = ExtractToDotNetRoot downloadPath dotnetRoot

            return dotnetRootDirInfo
        }

    let RunDotNetCommand (dotNetRoot: string)
                         (argument: string)
                         (workDirectory: string)
                         (environment: Dictionary<string, string>)
                         (waitForExit: bool) 
                         (silentRun: bool) =
        let executableExtension = GetExcutableFileExtensionByRID CurrentRID
        let dotnetExecutable = Path.Combine(dotNetRoot, $"dotnet{executableExtension}")

        environment["DOTNET_ROOT"] <- dotNetRoot
        let commandInvoker = new CommandInvoker(environment,
                                                workDirectory,
                                                dotnetExecutable,
                                                argument,
                                                silentRun)
        try
            commandInvoker.Proc.Start() |> ignore
            commandInvoker.Proc.BeginOutputReadLine()
            commandInvoker.Proc.BeginErrorReadLine()

            if waitForExit
            then
                commandInvoker.Proc.WaitForExit() |> ignore
        
        with ex -> 
            commandInvoker.Exception <- ex

        commandInvoker
    