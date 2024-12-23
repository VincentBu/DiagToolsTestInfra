namespace DiagToolsValidator.Core.CoreFunctionality

open System
open System.Diagnostics
open System.Text
open System.Collections.Generic
open System.Threading


module CommandLineTool =
    type CommandInvoker (environment: Dictionary<string, string>,
                         workDirectory: string,
                         fileName: string,
                         argument: string,
                         redirectStdOutErr: bool) =
        let _stdout = new StringBuilder()
        let _stderr = new StringBuilder()

        let proc = new Process()

        do proc.StartInfo.FileName <- fileName
        do proc.StartInfo.Arguments <- argument
        do proc.StartInfo.UseShellExecute <- false
        do proc.StartInfo.RedirectStandardInput <- true
        do proc.StartInfo.RedirectStandardOutput <- redirectStdOutErr
        do proc.StartInfo.RedirectStandardError <- redirectStdOutErr
        do proc.StartInfo.WorkingDirectory <- workDirectory

        do proc.StartInfo.EnvironmentVariables.Clear()
        do proc.StartInfo.EnvironmentVariables["Path"] <- Environment.GetEnvironmentVariable("Path")
        do for key in environment.Keys do
            proc.StartInfo.EnvironmentVariables[key] <- environment[key]

        let outputDataRecorder (sender: obj) (args: DataReceivedEventArgs) =
            let line = args.Data
            if not (isNull line) then
                _stdout.AppendLine $"   {line}" |> ignore
        
        let errorDataRecorder (sender: obj) (args: DataReceivedEventArgs) =
            let line = args.Data
            if not (isNull line) then
                _stderr.AppendLine $"   {line}" |> ignore

        do 
            if redirectStdOutErr
            then 
                proc.OutputDataReceived.AddHandler(outputDataRecorder)
                proc.ErrorDataReceived.AddHandler(errorDataRecorder)
            
        member val Command: string = $"{fileName} {argument}" with get
        member val StandardOutput: StringBuilder = _stdout with get
        member val StandardError: StringBuilder = _stderr with get
        member val Proc: Process = proc with get
        

    let PrinteOutputData (sender: obj) (args: DataReceivedEventArgs) =
        let line = $"   {args.Data}"
        if not (isNull line) then
            printfn "%s" line


    let IgnoreOutputData (sender: obj) (args: DataReceivedEventArgs) = ignore()
      
      
    let PrintErrorData (sender: obj) (args: DataReceivedEventArgs) =
        let line = $"   {args.Data}"
        if not (isNull line) then
            printfn "%s" line


    let IgnoreErrorData (sender: obj) (args: DataReceivedEventArgs) = ignore()        


    let RunCommand (fileName: string)
                   (argument: string)
                   (workDirectory: string)
                   (environment: Dictionary<string, string>)
                   (redirectStdOutErr: bool)
                   (outputDataReceivedHandler: obj -> DataReceivedEventArgs -> unit) 
                   (errorDataReceivedHandler: obj -> DataReceivedEventArgs -> unit) 
                   (waitForExit: bool)=
        try
            let commandInvoker = new CommandInvoker(environment,
                                                    workDirectory,
                                                    fileName,
                                                    argument,
                                                    redirectStdOutErr)
            printfn "Run command: %s" commandInvoker.Command
            if redirectStdOutErr then
                commandInvoker.Proc.OutputDataReceived.AddHandler outputDataReceivedHandler
                commandInvoker.Proc.ErrorDataReceived.AddHandler errorDataReceivedHandler

            commandInvoker.Proc.Start() |> ignore
            if redirectStdOutErr then
                commandInvoker.Proc.BeginOutputReadLine()
                commandInvoker.Proc.BeginErrorReadLine()

            if waitForExit
            then 
                commandInvoker.Proc.WaitForExit()

            Choice1Of2 commandInvoker
        with ex ->
            ex.Data.Add("RunCommand", $"Fail to run command: {fileName} {argument}")
            Choice2Of2 ex
    

    let TerminateCommandInvoker (commandInvoker: CommandInvoker) =
        // make sure the process is started
        try
            commandInvoker.Proc.Kill(true)
            commandInvoker.Proc.WaitForExit()
            while not (commandInvoker.Proc.HasExited) do
                Thread.Sleep(1000)
            Choice1Of2 commandInvoker
        with ex ->
            ex.Data.Add("TerminateCommandInvoker", $"Fail to terminate process: {commandInvoker.Command}")
            Choice2Of2 ex