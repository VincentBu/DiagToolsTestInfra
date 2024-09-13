namespace DiagToolsValidationToolSet.Core.Function

open System.IO
open System.Threading
open DiagToolsValidationToolSet.Core.Utility
open DiagToolsValidationToolSet.Core.Configuration

module DiagToolsTest =
    
    let TestDotNetCounters (configuration: DiagToolsTestRun.DiagToolsTestRunConfiguration) =
        let monitor = new ComputationExpressionBuilder.FunctionMonitorBuilder()

        let ev = configuration.EnvironmentVariable
        let dotnet = configuration.DotnetBinPath
        let toolRoot = configuration.DiagToolRoot
        let toolVersion = configuration.DiagTool.DiagToolVersion
        
        let workingDirectory = configuration.TestResultFolder
        
        monitor {
            let! dllPath = DotNetTool.GetToolDll toolRoot "dotnet-counters" toolVersion
            let! state = TargetApp.RunWebapp configuration
            let syncCommandArgList = [
                "--help"; "list"; "ps"
            ]

            let asyncCommandArgList = [
                $"collect -p {state.Proc.Id} -o webapp_counter.csv";
                $"monitor -p {state.Proc.Id}";
            ]

            syncCommandArgList
            |> List.map (fun commandArg -> 
                Terminal.RunCommandSync ev workingDirectory dotnet $"{dllPath} {commandArg}")
            |> ignore

            asyncCommandArgList
            |> List.map (fun commandArg -> 
                monitor {
                    let! state = Terminal.RunCommandAsync ev workingDirectory dotnet $"{dllPath} {commandArg}"
                    do Thread.Sleep(10000)
                    do state.Proc.Kill()
                    do state.Proc.WaitForExit()
                    return state
                })
            |> ignore

            return None
        } |> ignore

        monitor {
            let appName = "console"
            let appRoot = Path.Combine(configuration.TestBed, appName)
            let! dllPath = DotNetTool.GetToolDll toolRoot "dotnet-counters" toolVersion
            let! consoleBin = DotNetApp.GetAppBin appName appRoot

            let asyncCommandArgList = [
                $"collect -- {consoleBin} -o console_counter.csv";
                $"monitor -- {consoleBin}";
            ]

            asyncCommandArgList
            |> List.map (fun commandArg -> 
                monitor {
                    let! state = Terminal.RunCommandAsync ev workingDirectory dotnet $"{dllPath} {commandArg}"
                    do Thread.Sleep(10000)
                    do state.Proc.Kill()
                    do state.Proc.WaitForExit()
                    return state
                })
            |> ignore

            return None
        } |> ignore
