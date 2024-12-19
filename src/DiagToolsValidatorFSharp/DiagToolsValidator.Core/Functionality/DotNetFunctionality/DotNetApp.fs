namespace DiagToolsValidator.Core.CoreFunctionality

open System
open System.IO
open System.Xml.Linq


module DotNetApp =
    type DotNetApp (dotNetRoot: string,
                    appTemplate: string,
                    appRoot: string) =
        let _dotNetRoot = dotNetRoot
        let _appTemplate = appTemplate
        let _appRoot = appRoot

        member val AppRoot: string = _appRoot with get

        new() =
            new DotNetApp("", "", "")

        member x.GetProjectFile = Directory.GetFiles(_appRoot, "*.csproj")[0]
        
        member x.GetTargetFramework =
            try
                let projectFile = x.GetProjectFile
                let xmlData = File.ReadAllText(projectFile)
                let doc = XDocument.Parse(xmlData)
                Choice1Of2 (doc.Root.Element("PropertyGroup").Element("TargetFramework").Value)
            with
            | ex -> 
                ex.Data.Add("DotNetApp.GetTargetFramework", $"Fail to get target framework from {_appRoot}")
                Choice2Of2 ex

        member x.GetSymbolFolder (buildConfig: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            trace {
                let! targetFramework = x.GetTargetFramework
                let appSymbolFolder = Path.Combine(_appRoot, "bin", buildConfig, targetFramework)
                if Path.Exists(appSymbolFolder)
                then 
                    return appSymbolFolder
                else 
                    let ex = new Exception("Fail to get .NET app symbol folder")
                    ex.Data.Add("DotNetApp.GetAppSymbolFolder", $"Expected symbol folder {appSymbolFolder} doesn't exsit")
                    return! Choice2Of2 ex
            }

        member x.GetAppExecutable (buildConfig: string) =
            let trace = new Core.ProgressTraceBuilder(null)
            let excutableFileExtension = DotNet.GetExcutableFileExtensionByRID DotNet.CurrentRID
            trace {
                let! symbolFolder = x.GetSymbolFolder(buildConfig)
                let projectFile = x.GetProjectFile
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
            
        member x.CreateApp () =
            DotNet.RunDotNetCommand dotNetRoot 
                                    $"new {_appTemplate} -o {_appRoot} --force"
                                    "" 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true

        member x.BuildApp (buildConfig: string) =
            DotNet.RunDotNetCommand dotNetRoot
                                    $"build -c {buildConfig}"
                                    appRoot 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true
                                    
        member x.PublishApp (buildConfig: string) =
            DotNet.RunDotNetCommand dotNetRoot
                                    $"publish -r {DotNet.CurrentRID} -c {buildConfig}"
                                    appRoot 
                                    true 
                                    CommandLineTool.PrinteOutputData 
                                    CommandLineTool.PrintErrorData 
                                    true
