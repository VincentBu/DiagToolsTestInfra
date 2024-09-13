namespace DiagToolsValidationToolSet.Core.Function

open System.IO
open System.Runtime.InteropServices
open System.Collections.Generic

open DiagToolsValidationToolSet.Core.Utility

module DotNetApp =
    let CreateDotNetApp (env: Dictionary<string, string>)
                        (dotnetBinPath: string)
                        (templateName: string)
                        (appRoot: string) =
        let result = Terminal.RunCommandSync env "" dotnetBinPath $"new {templateName} -o {appRoot}"
        match result with
        | Choice1Of2 _ -> Choice1Of2 appRoot
        | Choice2Of2 ex -> Choice2Of2 ex


    let BuildDotNetApp (env: Dictionary<string, string>)
                       (buildConfig: string)
                       (dotnetBinPath: string)
                       (appRoot: string) =
                       
        let result = 
            if Common.IsNullOrEmptyString buildConfig
            then Terminal.RunCommandSync env appRoot dotnetBinPath $"build"
            else Terminal.RunCommandSync env appRoot dotnetBinPath $"build -c {buildConfig}"
        match result with
        | Choice1Of2 _ -> Choice1Of2 appRoot
        | Choice2Of2 ex -> Choice2Of2 ex


    let GetAppSymbolFolder (buildConfig: string) (appRoot: string) =
        let intermediateDirectory = Path.Combine(appRoot, "bin", buildConfig)
        try
            let symbolFolder =
                Directory.GetDirectories(intermediateDirectory, "net*")
                |> Array.head
            Choice1Of2 symbolFolder
        with ex -> Choice2Of2 (new exn($"GetAppSymbolFolder: Fail to find symbol folder in {appRoot}: {ex.Message}"))


    let GetAppILDll (appName: string) (dllFolder: string) =
        let appILDLL = Path.Combine(dllFolder, $"{appName}.dll")
        if Path.Exists appILDLL
        then Choice1Of2 appILDLL
        else Choice2Of2 (new exn($"GetAppILDll: Fail to find IL dll in {dllFolder}"))


    let GetAppBin (appName: string) (dllFolder: string) =
        let appBin = 
            if RuntimeInformation.RuntimeIdentifier.Contains "win"
            then Path.Combine(dllFolder, $"{appName}.exe")
            else Path.Combine(dllFolder, $"{appName}")
        if Path.Exists appBin
        then Choice1Of2 appBin
        else Choice2Of2 (new exn($"GetAppBin: Fail to find app bin file in {dllFolder}"))
    

    let StartDotNetAppByBin (env: Dictionary<string, string>)
                            (buildConfig: string)
                            (appRoot: string)
                            (appName: string)
                            (argument: string): Choice<Terminal.CommandRunResult, exn> =
        
        let monitor = new ComputationExpressionBuilder.FunctionMonitorBuilder()
        let GetAppSymbolFolder = GetAppSymbolFolder buildConfig
        let GetAppBin = GetAppBin appName

        let appExecutable = 
            monitor {
                let! appSymbolFolder = GetAppSymbolFolder appRoot
                let! appBin = GetAppBin appSymbolFolder
                return appBin
            }
        
        match appExecutable with
        | Choice1Of2 appBin -> Terminal.RunCommandAsync env "" appBin argument
        | Choice2Of2 ex -> Choice2Of2 ex
