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


    let CopyFile (source: string) (destination: string) =
        let realDestinationPath =
            if Directory.Exists destination
            then
                let fileName = Path.GetFileName(source)
                Path.Combine(destination, fileName)
            else
                destination

        try
            File.Copy(source, realDestinationPath)
            Choice1Of2 (0 |> ignore)
        with ex -> Choice2Of2 (new exn($"CopyFile: Fail to copy {source} to {realDestinationPath}: {ex.Message}"))


    let CreateDirectory (path: string) =
        try
            Choice1Of2 (Directory.CreateDirectory path)
        with ex -> Choice2Of2 (new exn($"CreateDirectory: Fail to create directory {path}: {ex.Message}"))
