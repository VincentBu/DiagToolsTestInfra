namespace DiagToolsValidationToolSet.Core.Utility

module ComputationExpressionBuilder =
    type FunctionMonitorBuilder() =
        member this.Bind(x, f) =
            match x with 
            | Choice2Of2 _ -> x
            | Choice1Of2 arg -> 
                let r = f arg
                r

        member this.Return(x) =
            Choice1Of2 x

        member this.ReturnFrom(x) =
            x
