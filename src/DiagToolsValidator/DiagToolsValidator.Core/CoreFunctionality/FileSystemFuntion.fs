namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Reflection
open System.IO.Compression
open System.Formats.Tar

open Core

module FileSystemFuntion =
    let CopyEmbeddedFile (resourceName: string) (destination: string) =
        try
            let assembly = Assembly.GetExecutingAssembly()
            use sr = assembly.GetManifestResourceStream(resourceName)
            use sw = File.OpenWrite(destination)
            sr.CopyTo(sw)
            Choice1Of2 (FileInfo destination)
        with ex ->
            Choice2Of2 ex


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
            Choice1Of2 (FileInfo realDestinationPath)
        with ex ->
            Choice2Of2 ex


    let CreateDirectory (path: string) =
        try
            Choice1Of2 (Directory.CreateDirectory path)
        with ex -> Choice2Of2 ex


    let DecompressGzippedTar (gzipPath: string) (destinationFolder: string) =
        let tarPath = Path.Combine(destinationFolder, "")
        try
            use originalFileStream = File.OpenRead gzipPath
            use decompressedFileStream = File.Create tarPath
            use decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress)
            decompressionStream.CopyTo(decompressedFileStream)
            TarFile.ExtractToDirectory(tarPath, destinationFolder, true)
            File.Delete tarPath
            Choice1Of2 (DirectoryInfo destinationFolder)
        with ex -> Choice2Of2 ex


    let DecompressZip (zipPath: string) (destinationFolder: string) =
        try
            ZipFile.ExtractToDirectory(zipPath, destinationFolder)
            Choice1Of2 (DirectoryInfo destinationFolder)
        with ex -> Choice2Of2 ex