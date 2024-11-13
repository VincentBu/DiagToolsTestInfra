namespace DiagToolsValidator.Core.CoreFunctionality

open System.IO
open System.Reflection
open System.IO.Compression
open System.Formats.Tar

open TypeDefinition

module Common =
    let IsNullOrEmptyString (str: string) : bool = 
        if str = null || str.Length = 0
        then true
        else false


    let CopyEmbeddedFile (resourceName: string) (destination: string) : IOAction<unit> =
        let assembly = Assembly.GetExecutingAssembly()
        use sr = assembly.GetManifestResourceStream(resourceName)
        use sw = File.OpenWrite(destination)
        IOAction (fun () -> sr.CopyTo(sw))


    let CopyFile (source: string) (destination: string) : IOAction<unit> =
        let realDestinationPath =
            if Directory.Exists destination
            then
                let fileName = Path.GetFileName(source)
                Path.Combine(destination, fileName)
            else
                destination
        IOAction (fun () -> File.Copy(source, realDestinationPath))


    let CreateDirectory (path: string) : IOAction<DirectoryInfo> =
        IOAction (fun () -> Directory.CreateDirectory path)


    let DecompressGzip (gzipPath: string) (destination: string) =
        IOAction (fun () -> 
                    use originalFileStream = File.OpenRead gzipPath
                    use decompressedFileStream = File.Create destination
                    use decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress)
                    decompressionStream.CopyTo(decompressedFileStream))


    let DecompressTar (tarPath: string) (destinationFolder: string) = 
        IOAction (fun () -> 
            TarFile.ExtractToDirectory(tarPath, destinationFolder, true))


    let DecompressZip (zipPath: string) (destinationFolder: string) = 
        IOAction (fun () -> 
            ZipFile.ExtractToDirectory(zipPath, destinationFolder))