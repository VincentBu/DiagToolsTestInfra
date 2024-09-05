namespace DiagToolsValidationToolSet.Core.Utility

open System.Collections.Generic
open System.Diagnostics

module Terminal =
    type CommandRunResult() =
        member val Command: string = null with get, set
        member val StandardOutput: string = "" with get, set
        member val StandardError: string = "" with get, set
        member val Proc: Process = null with get, set

    let RunCommandSync (env: Dictionary<string, string>) (fileName: string) (argument: string) =
        let command = $"{fileName} {argument}"
        try
            let result = new CommandRunResult()
            result.Command <- command

            use proc = new Process()
            proc.StartInfo.FileName <- fileName
            proc.StartInfo.Arguments <- argument
            proc.StartInfo.RedirectStandardOutput <- true
            proc.StartInfo.RedirectStandardError <- true
            proc.StartInfo.UseShellExecute <- false

            for kp in env do
                proc.StartInfo.EnvironmentVariables[kp.Key] <- kp.Value

            proc.OutputDataReceived.Add(fun _ -> 
                let line = proc.StandardOutput.ReadLine()
                result.StandardOutput <- $"{result.StandardOutput}{line}\n"
                printfn "%A" line)
            proc.ErrorDataReceived.Add(fun _ -> 
                let line = proc.StandardError.ReadLine()
                result.StandardError <- $"{result.StandardError}{line}\n"
                printfn "%A" line)
        
            proc.Start() |> ignore
            proc.BeginOutputReadLine()
            proc.BeginErrorReadLine()
            proc.WaitForExit()

            result.Proc <- proc
            if proc.ExitCode.Equals 0
            then Choice1Of2 result
            else Choice2Of2 (new exn($"RunCommandSync: Command {command} exit with {proc.ExitCode}."))
        with ex -> Choice2Of2 (new exn($"RunCommandSync: Fail to run command: {command}: {ex.Message}"))