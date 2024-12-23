namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Net.Http
open System.Diagnostics
open System.Runtime.InteropServices
open System.Collections.Generic

open Core
open CommandLineTool

module DotNet =
    let CurrentRID = RuntimeInformation.RuntimeIdentifier
    
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

            if not response.IsSuccessStatusCode
            then raise(new exn(response.ReasonPhrase))

            let! content = response.Content.ReadAsStringAsync()
            let productVersion = content.Replace("\n", "").Replace("\r", "")
            return $"{feed}/Sdk/{sdkFullVersion}/dotnet-sdk-{productVersion}-{rid}{SDKExtension}"
        }
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.RunSynchronously
        
        
    let DownloadCompressedDotNetSDK (downloadLink: string) (downloadPath: string) =
        use httpClient = new HttpClient()
        task {
            let! downloadSDKResponse = httpClient.GetAsync(downloadLink)
            if not downloadSDKResponse.IsSuccessStatusCode
            then
                raise(new exn(downloadSDKResponse.ReasonPhrase))

            let! content = downloadSDKResponse.Content.ReadAsByteArrayAsync()
            do! File.WriteAllBytesAsync(downloadPath, content)
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
            Choice2Of2 (new exn($"Unsupported compressed file type: {extension}"))


    let InstallDotNetSDKByVersion (rid: string) (sdkFullVersion: string) (dotnetRoot: string) =
        let AzureFeedList = [
            "https://dotnetcli.azureedge.net/dotnet";
            "https://dotnetbuilds.azureedge.net/public"
        ]
        let tarce = new Core.ProgressTraceBuilder(null)
        let SDKExtension = GetCompressionExtensionByRID rid
        let downloadPath = Path.GetTempFileName() + SDKExtension
        tarce {
            let! downloadLink = 
                tarce {
                    for feed in AzureFeedList do
                        let! downloadLink = GenerateDownloadLink feed rid sdkFullVersion 
                        return downloadLink
                }
 
            let! downloadPath = DownloadCompressedDotNetSDK downloadLink downloadPath
            
            let! dotnetRootDirInfo = ExtractToDotNetRoot downloadPath dotnetRoot
            return dotnetRootDirInfo
        }


    let RunDotNetCommand (dotNetEnv: Dictionary<string, string>)
                         (argument: string)
                         (workDirectory: string)
                         (redirectStdOutErr: bool)
                         (outputDataReceivedHandler: obj -> DataReceivedEventArgs -> unit) 
                         (errorDataReceivedHandler: obj -> DataReceivedEventArgs -> unit) 
                         (waitForExit: bool)=
        let executableExtension = GetExcutableFileExtensionByRID CurrentRID
        let dotNetExecutable = Path.Combine(dotNetEnv["DOTNET_ROOT"], $"dotnet{executableExtension}")

        RunCommand dotNetExecutable 
                   argument
                   workDirectory 
                   dotNetEnv 
                   redirectStdOutErr 
                   outputDataReceivedHandler 
                   errorDataReceivedHandler 
                   waitForExit