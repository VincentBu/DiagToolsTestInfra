namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Collections.Generic

open Core
open DotNet

module DotNetApp =

    let CreateDotNetApp (env: Dictionary<string, string>)
                        (dotNetRoot: string)
                        (templateName: string)
                        (appRoot: string) =
        RunDotNetCommand dotNetRoot $"new {templateName} -o {appRoot}" "" env false true
        

    let BuildDotNetApp (env: Dictionary<string, string>)
                       (dotNetRoot: string)
                       (buildConfig: string)
                       (appRoot: string) =
        RunDotNetCommand dotNetRoot $"build -c {buildConfig}" appRoot env false true
        

    let PublishDotNetApp (env: Dictionary<string, string>)
                         (dotNetRoot: string)
                         (buildConfig: string)
                         (appRoot: string) =
        RunDotNetCommand dotNetRoot $"publish -r {CurrentRID} -c {buildConfig}" appRoot env false true


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


    let GetAppExecutable (appName: string) (dllFolder: string) =
        let executableExtension = GetExcutableFileExtensionByRID CurrentRID
        let appExecutable = Path.Combine(dllFolder, $"{appName}{executableExtension}")
        if Path.Exists appExecutable
        then Choice1Of2 appExecutable
        else Choice2Of2 (new exn($"GetAppBin: Fail to find app bin file in {dllFolder}"))
    

    let StartDotNetAppByExecutable (env: Dictionary<string, string>)
                                   (workingDirectory: string)
                                   (buildConfig: string)
                                   (appRoot: string)
                                   (appName: string)
                                   (argument: string)
                                   (waitToExit: bool)
                                   (silentRun: bool) =
        
        let choice = new ChoiceChainBuilder()

        let commandInvoker = 
            choice {
                let! appSymbolFolder = GetAppSymbolFolder buildConfig appRoot
                let! appExecutable = GetAppExecutable appName appSymbolFolder
                let commandInvoker = RunCommand appExecutable argument workingDirectory env waitToExit silentRun
                return commandInvoker
            }
        
        commandInvoker
