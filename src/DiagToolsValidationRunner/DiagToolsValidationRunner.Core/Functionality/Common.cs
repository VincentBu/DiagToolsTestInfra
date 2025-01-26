using System.Formats.Tar;
using System.IO.Compression;

namespace DiagToolsValidationRunner.Core.Functionality
{
    public static class Utilities
    {
        public static void CopyFile(string srcPath, string dstPath)
        {
            string realDestPath = String.Empty;
            if (Directory.Exists(dstPath))
            {
                // Copy file to a directory
                string fileName = Path.GetFileName(srcPath);
                realDestPath = Path.Combine(dstPath, fileName);
            }
            else
            {
                realDestPath = dstPath;
            }

            File.Copy(srcPath, realDestPath);
        }

        public static void DecompressGzippedTar(string gzipTarPath, string destinationFolder)
        {
            string tarPath = Path.GetTempFileName();
            using (FileStream originalFileStream = File.OpenRead(gzipTarPath))
            {
                using (FileStream decompressedFileStream = File.Create(tarPath))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }

            Directory.CreateDirectory(destinationFolder);
            TarFile.ExtractToDirectory(tarPath, destinationFolder, true);
            File.Delete(tarPath);
        }

        public static void DecompressZip(string zipPath, string destinationFolder)
        {
            ZipFile.ExtractToDirectory(zipPath, destinationFolder, true);
        }

        public static async Task Download(string downloadLink, string downloadPath)
        {
            using (HttpClient httpClient = new())
            {
                HttpResponseMessage response = await httpClient.GetAsync(downloadLink);
                response.EnsureSuccessStatusCode();
                using (Stream reader = await response.Content.ReadAsStreamAsync())
                {
                    using (Stream writer = File.OpenWrite(downloadPath))
                    {
                        await reader.CopyToAsync(writer);
                    }
                }
            }
        }
    }
}
