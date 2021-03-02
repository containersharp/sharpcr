using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Registry;

namespace SharpCR.Features.LocalStorage
{
    public class DiskBlobStorage : IBlobStorage
    {
        private readonly string _storageBasePath;

        public DiskBlobStorage(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            var config = configuredOptions.Value ?? new LocalStorageConfiguration();
            var contentRoot = config.BasePath ?? environment.WebRootPath;
            _storageBasePath = Path.Combine(contentRoot, config.BlobsDirectoryName);
        }
        
        public Stream Read(string location)
        {
            var path = MapPath(location);
            return File.Exists(path) ? File.OpenRead(path) : null;
        }

        public void Delete(string location)
        {
            var path = MapPath(location);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string Save(string repoName, string digest, Stream stream)
        {
            var location = Path.Combine(repoName, digest.Replace('@', Path.DirectorySeparatorChar));
            var savePath = MapPath(location);
            
            var directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            using var outputStream = File.Create(savePath);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(outputStream);
            
            return savePath.Substring(_storageBasePath.Length).Trim(Path.DirectorySeparatorChar);
        }

        public bool SupportsDownloading { get; } = false;
        public string GenerateDownloadUrl(string location)
        {
            throw new System.NotImplementedException();
        }

        private string MapPath(string location)
        {
            var canonicalSubPath = location
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Trim(Path.DirectorySeparatorChar);
            
            return Path.Combine(_storageBasePath, canonicalSubPath);
        }
    }
}