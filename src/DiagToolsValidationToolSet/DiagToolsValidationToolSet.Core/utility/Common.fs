namespace DiagToolsValidationToolSet.Core.Utility

open System.IO
open System.Reflection

module Common =
    let IsNullOrEmptyString (str: string) = 
        if str = null || str.Length = 0
        then true
        else false


    let CopyEmbeddedFile (resourceName: string) (destination: string) =
        try
            let assembly = Assembly.GetExecutingAssembly()
            use sr = assembly.GetManifestResourceStream(resourceName)
            use sw = File.OpenWrite(destination)
            sr.CopyTo(sw)
            Choice1Of2 (0 |> ignore)
        with ex -> Choice2Of2 (new exn($"CopyEmbeddedFile: Fail to copy embedded file: {ex.Message}"))