namespace DiagToolsValidationToolSet.Core.Function

open System.IO
open DiagToolsValidationToolSet.Core.Configuration
open DiagToolsValidationToolSet.Core.Utility
open System.Threading

module TargetApp =
    let monitor = new ComputationExpressionBuilder.FunctionMonitorBuilder()
    
    let currentNamespace = "DiagToolsValidationToolSet.Core.Function"
    
    let CreateBuildConsoleApp (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let ev = configuration.EnvironmentVariable
        let dotnet = configuration.DotnetBinPath
        let appName = "console"
        let appTemplateName = "console"
        let appRoot = Path.Combine(configuration.TestBed, appName)

        let CreateConsoleApp = DotNetApp.CreateDotNetApp ev dotnet appTemplateName
        
        let ReplaceSourceFileForProject (appRoot: string) = 
            let f = Common.CopyEmbeddedFile $"{currentNamespace}.Resources.consoleapp.Program.cs"
            let destPath = Path.Combine(appRoot, "Program.cs")
            let result = f destPath
            match result with
            | Choice1Of2 _ -> Choice1Of2 appRoot
            | Choice2Of2 ex -> Choice2Of2 ex

        let BuildConsoleApp = DotNetApp.BuildDotNetApp ev "" dotnet

        monitor {
            let! appRoot = CreateConsoleApp appRoot
            let! appRoot = ReplaceSourceFileForProject appRoot
            let! appRoot = BuildConsoleApp appRoot
            return appRoot 
        }


    let CreateBuildWebApp (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let ev = configuration.EnvironmentVariable
        let dotnet = configuration.DotnetBinPath
        let appName = "webapp"
        let appTemplateName = "webapp"
        let appRoot = Path.Combine(configuration.TestBed, appName)

        let CreateConsoleApp = DotNetApp.CreateDotNetApp ev dotnet appTemplateName

        let BuildConsoleApp = DotNetApp.BuildDotNetApp ev "" dotnet

        monitor {
            let! appRoot = CreateConsoleApp appRoot
            let! appRoot = BuildConsoleApp appRoot
            return appRoot 
        }


    let RunWebapp (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let ev = configuration.EnvironmentVariable
        let appName = "webapp"
        let appRoot = Path.Combine(configuration.TestBed, appName)
        
        let state = DotNetApp.StartDotNetAppByBin ev "Debug" appRoot appName "" 

        match state with
        | Choice2Of2 ex -> Choice2Of2 ex
        | Choice1Of2 commandRunResult -> 
            while not (commandRunResult.StandardOutput.Contains("Application started")) do
                Thread.Sleep(2000)

            Choice1Of2 commandRunResult


    let CreateBuildGCDumpPlayground (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let ev = configuration.EnvironmentVariable
        let dotnet = configuration.DotnetBinPath
        let appName = "GCDumpPlayground2"
        let appTemplateName = "console"
        let appRoot = Path.Combine(configuration.TestBed, appName)

        let CreateConsoleApp = DotNetApp.CreateDotNetApp ev dotnet appTemplateName
        
        let ReplaceSourceFileForProject (appRoot: string) = 
            let f = Common.CopyEmbeddedFile $"{currentNamespace}.Resources.GCDumpPlayground2.Program.cs"
            let destPath = Path.Combine(appRoot, "Program.cs")
            let result = f destPath
            match result with
            | Choice1Of2 _ -> Choice1Of2 appRoot
            | Choice2Of2 ex -> Choice2Of2 ex

        let BuildConsoleApp = DotNetApp.BuildDotNetApp ev "" dotnet

        monitor {
            let! appRoot = CreateConsoleApp appRoot
            let! appRoot = ReplaceSourceFileForProject appRoot
            let! appRoot = BuildConsoleApp appRoot
            return appRoot 
        }


    let RunGCDumpPlayground (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let ev = configuration.EnvironmentVariable
        let appName = "GCDumpPlayground2"
        let appRoot = Path.Combine(configuration.TestBed, appName)
        
        let state = DotNetApp.StartDotNetAppByBin ev "Debug" appRoot appName "" 

        match state with
        | Choice2Of2 ex -> Choice2Of2 ex
        | Choice1Of2 commandRunResult -> 
            while not (commandRunResult.StandardOutput.Contains("Pause for gcdumps")) do
                Thread.Sleep(2000)

            Choice1Of2 commandRunResult