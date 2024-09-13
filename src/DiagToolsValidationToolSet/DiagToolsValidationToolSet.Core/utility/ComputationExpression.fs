namespace DiagToolsValidationToolSet.Core.Utility

module ComputationExpressionBuilder =
    type FunctionMonitorBuilder() =
        member this.Bind(x, f) =
            match x with 
            | Choice2Of2 (ex:exn) -> Choice2Of2 ex
            | Choice1Of2 arg -> 
                let r = f arg
                r

        member this.Return(x) =
            Choice1Of2 x

        member this.ReturnFrom(x) =
            x
            
        member this.Zero() =
            None

        member this.Combine(a,b) =
            match a, b with
            | Choice1Of2 x, Choice1Of2 y -> List.concat [x; y]
            | Choice1Of2 x, Choice2Of2 y -> List.concat [x;]
            | Choice2Of2 x, Choice1Of2 y -> List.concat [y;]
            | Choice2Of2 x, Choice2Of2 y -> List.concat []

        member this.For(collection, f) =
            collection |> List.map f

        member this.Delay(f) =
            f()
