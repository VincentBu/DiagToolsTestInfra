﻿namespace DiagToolsValidator.Core.Functionality

open System
open System.IO
open System.Xml.Linq
open System.Collections.Generic


module DotNetApp =
    let GetProjectFile (appRoot: string) =
        let projectFileCandidates = Directory.GetFiles(appRoot, "*.csproj")
        if Array.length projectFileCandidates = 0
        then Choice2Of2 (new exn("No project file available"))
        else Choice1Of2 (Array.head projectFileCandidates)


    let GetTargetFramework (appRoot: string) =
        let trace = new Core.ProgressTraceBuilder(null)
        trace {
            let! projectFile = GetProjectFile appRoot
            try
                let xmlData = File.ReadAllText(projectFile)
                let doc = XDocument.Parse(xmlData)
                return! Choice1Of2 (doc.Root.Element("PropertyGroup").Element("TargetFramework").Value)
            with
            | ex -> 
                ex.Data.Add("DotNetApp.GetTargetFramework", $"Fail to get target framework from {appRoot}")
                return! Choice2Of2 ex
        }


    let GetSymbolFolder (appRoot: string) (buildConfig: string) (targetRID: string) =
        let trace = new Core.ProgressTraceBuilder(null)
        trace {
            let! targetFramework = GetTargetFramework appRoot
            let appSymbolFolder = Path.Combine(appRoot, "bin", buildConfig, targetFramework, targetRID)
            if Path.Exists(appSymbolFolder)
            then 
                return appSymbolFolder
            else 
                let ex = new Exception("Fail to get .NET app symbol folder")
                ex.Data.Add("DotNetApp.GetAppSymbolFolder", $"Expected symbol folder {appSymbolFolder} doesn't exsit")
                return! Choice2Of2 ex
        }

    type DotNetApp (dotNetEnv: Dictionary<string, string>,
                    appTemplate: string,
                    appRoot: string) =
        let _dotNetEnv = dotNetEnv
        let _appTemplate = appTemplate
        let _appRoot = appRoot

        member val AppRoot: string = _appRoot with get

        new() =
            let env = new Dictionary<string, string>()
            new DotNetApp(env, "", "")

        member x.GetProjectFile = GetProjectFile _appRoot
        
        member x.GetTargetFramework = GetTargetFramework _appRoot

        member x.GetSymbolFolder (buildConfig: string) (targetRID: string) =
            GetSymbolFolder _appRoot buildConfig targetRID

        member x.GetNativeSymbolFolder (buildConfig: string) (targetRID: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            trace {
                let! targetFramework = x.GetTargetFramework
                let appSymbolFolder = Path.Combine(_appRoot, "bin", buildConfig, targetFramework, targetRID, "publish")
                if Path.Exists(appSymbolFolder)
                then 
                    return appSymbolFolder
                else 
                    let ex = new Exception("Fail to get .NET app native symbol folder")
                    ex.Data.Add("DotNetApp.GetNativeSymbolFolder", $"Expected native symbol folder {appSymbolFolder} doesn't exsit")
                    return! Choice2Of2 ex
            }

        member x.GetAppExecutable (buildConfig: string) (targetRID: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            let excutableFileExtension = DotNet.GetExcutableFileExtensionByRID targetRID
            trace {
                let! symbolFolder = x.GetSymbolFolder buildConfig targetRID
                let! projectFile = x.GetProjectFile
                let appName = Path.GetFileNameWithoutExtension(projectFile)
                let excutable = Path.Combine(symbolFolder, $"{appName}{excutableFileExtension}")
                if Path.Exists(excutable)
                then 
                    return excutable
                else 
                    let ex = new Exception("Fail to get .NET app excutable file")
                    ex.Data.Add("DotNetApp.GetAppExecutable", $"Excutable file {excutable} doesn't exsit")
                    return! Choice2Of2 ex
            }
            
        member x.GetAppNativeExecutable (buildConfig: string) (targetRID: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            let excutableFileExtension = DotNet.GetExcutableFileExtensionByRID targetRID
            trace {
                let! symbolFolder = x.GetNativeSymbolFolder buildConfig targetRID
                let! projectFile = x.GetProjectFile
                let appName = Path.GetFileNameWithoutExtension(projectFile)
                let excutable = Path.Combine(symbolFolder, $"{appName}{excutableFileExtension}")
                if Path.Exists(excutable)
                then 
                    return excutable
                else 
                    let ex = new Exception("Fail to get .NET app native excutable file")
                    ex.Data.Add("DotNetApp.GetAppNativeExecutable", $"Native excutable file {excutable} doesn't exsit")
                    return! Choice2Of2 ex
            }

        member x.GetCreateDump (buildConfig: string) (targetRID: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            let excutableFileExtension = DotNet.GetExcutableFileExtensionByRID targetRID
            trace {
                let! symbolFolder = x.GetSymbolFolder buildConfig targetRID
                let createDump = Path.Combine(symbolFolder, $"createdump{excutableFileExtension}")
                if Path.Exists(createDump)
                then 
                    return createDump
                else 
                    let ex = new Exception("Fail to get createadump for .NET app")
                    ex.Data.Add("DotNetApp.GetCreateDump", $"{createDump} doesn't exsit")
                    return! Choice2Of2 ex
            }
            
        member x.CreateApp () =
            DotNet.RunDotNetCommand _dotNetEnv 
                                    $"new {_appTemplate} -o {_appRoot} --force"
                                    "" 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true

        member x.BuildApp (buildConfig: string) (targetRID: string) =
            DotNet.RunDotNetCommand _dotNetEnv
                                    $"build -r {targetRID} -c {buildConfig}"
                                    appRoot 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true
                                    
        member x.PublishApp (buildConfig: string) (targetRID: string) =
            DotNet.RunDotNetCommand _dotNetEnv
                                    $"publish -r {targetRID} -c {buildConfig}"
                                    appRoot 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true
