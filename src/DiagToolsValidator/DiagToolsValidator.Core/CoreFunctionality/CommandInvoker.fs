namespace DiagToolsValidationToolSet.Core.Utility

open System.Collections.Generic
open System.Diagnostics

open DiagToolsValidator.Core.CoreFunctionality.TypeDefinition

module CommandInvoker =

    let InvokeCommand (env: Dictionary<string, string>) (workingDirectory: string) (fileName: string) (argument: string) =
        let runinfo = new CommandRunInfo()
        runinfo.Command <- $"{fileName} {argument}"
        runinfo.StandardOutput <- ""
        runinfo.StandardError <- ""

        use proc = new Process()
        proc.StartInfo.FileName <- fileName
        proc.StartInfo.Arguments <- argument
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true
        proc.StartInfo.UseShellExecute <- false

        proc.StartInfo.WorkingDirectory <- workingDirectory

        for kp in env do
            proc.StartInfo.EnvironmentVariables[kp.Key] <- kp.Value

        proc.OutputDataReceived.Add(fun _ -> 
            let line = proc.StandardOutput.ReadLine()
            runinfo.StandardOutput <- $"{runinfo.StandardOutput}{line}\n"
            printfn "%A" line)
        proc.ErrorDataReceived.Add(fun _ -> 
            let line = proc.StandardError.ReadLine()
            runinfo.StandardError <- $"{runinfo.StandardError}{line}\n"
            printfn "%A" line)
        
        runinfo
