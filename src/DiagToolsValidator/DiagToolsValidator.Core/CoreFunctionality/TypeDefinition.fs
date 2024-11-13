namespace DiagToolsValidator.Core.CoreFunctionality

open System.Collections.Generic
open System.Diagnostics

module TypeDefinition =
    type IOAction<'a> = IOAction of (unit -> 'a)
    
    type CommandRunInfo() =
        member val Command: string = null with get, set
        member val StandardOutput: string = "" with get, set
        member val StandardError: string = "" with get, set
        member val Proc: Process = null with get, set

    type ChoiceChainBuilder() =
        member this.Bind(x: Choice<'a, exn>, f) =
            match x with 
            | Choice2Of2 (ex:exn) -> Choice2Of2 ex
            | Choice1Of2 arg ->  f arg

        member this.Bind(x, f: 'a -> IOAction<'b>) =
            let (IOAction io) = f x
            try
                let ioResult = io()
                Choice1Of2 ioResult
            with ex -> Choice2Of2 ex

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

