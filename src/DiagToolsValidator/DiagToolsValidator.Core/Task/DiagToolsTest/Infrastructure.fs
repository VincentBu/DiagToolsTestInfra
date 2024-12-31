namespace DiagToolsValidator.Core.Task.DiagToolsTest

open System
open System.IO
open System.Threading

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration


module TestInfrastructure =
    let GenerateEnvironmentActivationScript(configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let scriptPath, lines =
            if DotNet.CurrentRID.Contains "win"
            then Path.Combine(configuration.TestBed, "env_activation.ps1"), [
                $"$Env:DOTNET_ROOT=\"{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DotNet.DotNetRoot}\"";
                $"$Env:Path+=\";{configuration.DiagTool.ToolRoot}\""
            ]
            else Path.Combine(configuration.TestBed, "env_activation.sh"),[
                $"export DOTNET_ROOT={configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DotNet.DotNetRoot}";
                $"export PATH=$PATH:{configuration.DiagTool.ToolRoot}"
            ]

        try
            File.WriteAllLines(scriptPath, lines)
            Choice1Of2 scriptPath
        with ex -> 
            ex.Data.Add("", "Fail to generate environment activation script.")
            Choice2Of2 ex


    let RunWebapp (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)
        let invokeResult = 
            trace {
                let! executablePath = configuration.TargetApp.WebApp.GetAppExecutable(configuration.TargetApp.BuildConfig)
                let! commandInvoker = CommandLineTool.RunCommand executablePath
                                                                 ""
                                                                 ""
                                                                 configuration.SystemInfo.EnvironmentVariables
                                                                 true
                                                                 CommandLineTool.IgnoreOutputData
                                                                 CommandLineTool.IgnoreErrorData
                                                                 false
                return commandInvoker
            }

        match invokeResult with
        | Choice2Of2 _ -> ignore()
        | Choice1Of2 invoker ->
            if String.IsNullOrEmpty(invoker.StandardError.ToString()) 
            then
                while not(invoker.StandardOutput.ToString().Contains("Application started")) do
                    Thread.Sleep(1000)

        invokeResult


    let RunGCDumpPlayground (configuration: DiagToolsTestConfiguration.DiagToolsTestConfiguration) =
        let trace = new Core.ProgressTraceBuilder(null)
        let invokeResult = 
            trace {
                let! executablePath = configuration.TargetApp.GCDumpPlayground.GetAppExecutable(configuration.TargetApp.BuildConfig)
                let! commandInvoker = CommandLineTool.RunCommand executablePath
                                                                 "0.05"
                                                                 ""
                                                                 configuration.SystemInfo.EnvironmentVariables
                                                                 true
                                                                 CommandLineTool.IgnoreOutputData
                                                                 CommandLineTool.IgnoreErrorData
                                                                 false

                return commandInvoker
            }

        match invokeResult with
        | Choice2Of2 _ -> ignore()
        | Choice1Of2 invoker ->
            if String.IsNullOrEmpty(invoker.StandardError.ToString()) 
            then
                while not(invoker.StandardOutput.ToString().Contains("Pause for gcdumps")) do
                    Thread.Sleep(1000)

        invokeResult