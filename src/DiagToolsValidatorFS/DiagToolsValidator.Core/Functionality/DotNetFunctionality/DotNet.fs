namespace DiagToolsValidator.Core.Functionality

open System.IO
open System.Net.Http
open System.Diagnostics
open System.Runtime.InteropServices
open System.Collections.Generic

open Core
open CommandLineTool
open Microsoft.Win32


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
        let IterateUntilSuccess(collection, f: 'a -> Choice<'b, exn>) =
            let failedItems = new List<Choice<'b, exn>>()

            let rec Iterate seq =
                match seq with
                | [] -> 
                    if failedItems.Count = 0
                    then
                        Choice2Of2 (new exn("No item in collection."))
                    else
                        failedItems[0]
                | x::xs -> 
                    let result = f x
                    match result with
                    | Choice1Of2 _ -> result
                    | Choice2Of2 ex -> 
                        failedItems.Add(Choice2Of2 ex)
                        Iterate xs
            Iterate collection

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
                     return! IterateUntilSuccess(
                        AzureFeedList,
                        fun feed -> GenerateDownloadLink feed rid sdkFullVersion)
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


    let ActiveDumpGeneratingEnvironment (env: Dictionary<string, string>) (dumpPath: string) =
        let _env = new Dictionary<string, string>(env)

        _env["DOTNET_DbgEnableMiniDump"] <- "1"
        _env["DOTNET_DbgMiniDumpType"] <- "4"
        _env["DOTNET_DbgMiniDumpName"] <- dumpPath

        _env


    let DeactiveDumpGeneratingEnvironment (env: Dictionary<string, string>) =
        let _env = new Dictionary<string, string>(env)
        _env["DOTNET_DbgEnableMiniDump"] <- "0"
        _env


    let ActiveNativeDumpGeneratingEnvironment (env: Dictionary<string, string>) (dumpFolder: string) =
        let _env = new Dictionary<string, string>(env)
        
        if CurrentRID.Contains("win")
        then 
            try
                // windows case: set reg keys
                let registrykeyHKLM = Registry.LocalMachine
                let LocalDumpsKeyPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps"
                let LocalDumpsKey = registrykeyHKLM.OpenSubKey(LocalDumpsKeyPath, true)
                LocalDumpsKey.SetValue("DumpFolder", dumpFolder, RegistryValueKind.ExpandString)
                LocalDumpsKey.SetValue("DumpType", 2, RegistryValueKind.DWord)
                LocalDumpsKey.Close()
                Choice1Of2 _env
            with ex ->
                ex.Data.Add("", $"Fail to set registry key.")
                Choice2Of2 ex
        else
            _env["DOTNET_DbgEnableMiniDump"] <- "1"
            _env["DOTNET_DbgMiniDumpType"] <- "4"
            _env["DOTNET_DbgMiniDumpName"] <- 
                Path.Combine(dumpFolder, $"nativedump.dmp")
            Choice1Of2 _env


    let DeactiveNativeDumpGeneratingEnvironment (env: Dictionary<string, string>) =
        let _env = new Dictionary<string, string>(env)
        if CurrentRID.Contains("win")
        then // windows case: delete reg keys
            try
                let registrykeyHKLM = Registry.CurrentUser
                let keyPath = @"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps"

                registrykeyHKLM.DeleteSubKey($@"{keyPath}\DumpFolder", false)
                registrykeyHKLM.DeleteSubKey($@"{keyPath}\DumpType", false)
                Choice1Of2 _env
            with ex ->
                ex.Data.Add("", $"Fail to delete registry key.")
                Choice2Of2 ex
        else
            _env["DOTNET_DbgEnableMiniDump"] <- "0"
            Choice1Of2 _env


    let ActiveStressLogEnvironment (env: Dictionary<string, string>) =
        let _env = new Dictionary<string, string>(env)
        _env["DOTNET_StressLogLevel"] <- "10"
        _env["DOTNET_TotalStressLogSize"] <- "8196"
        _env