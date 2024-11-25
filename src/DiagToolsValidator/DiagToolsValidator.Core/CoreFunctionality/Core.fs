namespace DiagToolsValidator.Core.CoreFunctionality

open System.Collections.Generic
open System.Diagnostics
open System.Text
open Microsoft.FSharp.Quotations

module Core =
    let rec GetFunctionName = function
        | Patterns.Call(None, methodInfo, _) -> methodInfo.Name
        | Patterns.Lambda(_, expr) -> GetFunctionName expr
        | _ -> failwith "Unexpected input"


    let IsNullOrEmptyString (str: string) : bool = 
        if str = null || str.Length = 0
        then true
        else false


    type CommandInvoker (environment: Dictionary<string, string>,
                         workDirectory: string,
                         fileName: string,
                         argument: string,
                         silentRun: bool) =
        let proc = new Process()
        let _StandardOutput = new StringBuilder()
        let _StandardError = new StringBuilder()

        do proc.StartInfo.FileName <- fileName
        do proc.StartInfo.Arguments <- argument
        do proc.StartInfo.UseShellExecute <- false
        do proc.StartInfo.RedirectStandardInput <- true
        do proc.StartInfo.RedirectStandardOutput <- true
        do proc.StartInfo.RedirectStandardError <- true
        do proc.StartInfo.WorkingDirectory <- workDirectory

        do for key in environment.Keys do
            proc.StartInfo.EnvironmentVariables[key] <- environment[key]

        let outputDataReceivedHandler (sender: obj) (args: DataReceivedEventArgs) =
            let line = args.Data
            if not (isNull line) then
                _StandardOutput.AppendLine line |> ignore
                if not silentRun then
                    printfn "%s" line

        let errorDataReceivedHandler (sender: obj) (args: DataReceivedEventArgs) =
            let line = args.Data
            if not (isNull line) then
                _StandardError.AppendLine line |> ignore
                if not silentRun then
                    printfn "%s" line
                
        do proc.OutputDataReceived.AddHandler outputDataReceivedHandler
        do proc.ErrorDataReceived.AddHandler errorDataReceivedHandler

        member val Command: string = $"{fileName} {argument}" with get
        member val StandardOutput: StringBuilder = _StandardOutput with get
        member val StandardError: StringBuilder = _StandardError with get
        member val Proc: Process = proc with get


    let RunCommand (fileName: string)
                   (argument: string)
                   (workDirectory: string)
                   (environment: Dictionary<string, string>)
                   (waitForExit: bool) 
                   (silentRun: bool) =
        let commandInvoker = new CommandInvoker(environment,
                                                workDirectory,
                                                fileName,
                                                argument,
                                                silentRun)

        commandInvoker.Proc.Start() |> ignore
        commandInvoker.Proc.BeginOutputReadLine()
        commandInvoker.Proc.BeginErrorReadLine()

        if waitForExit
        then
            commandInvoker.Proc.WaitForExit() |> ignore
        
        commandInvoker


    type ChoiceChainBuilder() =
        member this.Bind(x: Choice<'a, exn>, f: 'a -> Choice<'b, exn>) =
            let functionName = GetFunctionName <@ f @>
            match x with 
            | Choice2Of2 (ex: exn) -> Choice2Of2 ex
            | Choice1Of2 arg -> 
                let result = f arg
                match result with
                | Choice2Of2 (ex: exn) -> 
                    let errorMessage = $"{functionName}: {ex.Message}\n{ex.StackTrace}"
                    printfn "%s" errorMessage |> ignore
                    
                | _ -> ignore()
                result

        member this.Return(x) =
            Choice1Of2 x

        member this.ReturnFrom(x) =
            x
        
        member this.Zero() =
            Choice2Of2 (new exn("No return."))

        member this.For(collection, f) =
            let mem = new List<Choice<'a, exn>>()

            let rec EarlyReturnLoop seq =
                match seq with
                | [] -> 
                    if mem.Count = 0
                    then
                        Choice2Of2 (new exn("No item in collection."))
                    else
                        mem[0]
                | x::xs -> 
                    let result = f x
                    match result with
                    | Choice1Of2 _ -> result
                    | Choice2Of2 ex -> 
                        mem.Add(Choice2Of2 ex)
                        EarlyReturnLoop xs

            EarlyReturnLoop collection

        member this.Delay(funToDelay) = 
            funToDelay

        member this.Run(delayedFun) =
            delayedFun()

        member this.Combine(a, b) =
            match a with
            | Choice2Of2 ex ->  Choice2Of2 ex
            | Choice1Of2 _ -> b 
