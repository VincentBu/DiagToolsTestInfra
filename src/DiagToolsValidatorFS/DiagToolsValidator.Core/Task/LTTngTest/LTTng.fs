namespace DiagToolsValidator.Core.Task.LTTng

open System.IO

open DiagToolsValidator.Core.Functionality
open DiagToolsValidator.Core.Configuration

module LTTng =
    let TestLTTng (configuration: LTTngTestRunConfiguration.LTTngTestRunConfiguration) =
        let loggerPath = Path.Combine(configuration.TestBed, $"LTTng-{configuration.DotNet.SDKVersion}-{DotNet.CurrentRID}.txt")
        let trace = new Core.ProgressTraceBuilder(loggerPath)
        trace {
            let gcperfsimInvokeResult = TestInfrastructure.RunGCPerfsim configuration
            yield! gcperfsimInvokeResult

            let! gcperfsimInvoker = gcperfsimInvokeResult

            // Collect trace
            yield! TestInfrastructure.RunPerfcollect configuration

            CommandLineTool.TerminateCommandInvoker gcperfsimInvoker |> ignore
        }
