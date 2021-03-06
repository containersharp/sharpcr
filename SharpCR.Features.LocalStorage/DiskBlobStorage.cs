using System.IO;
using System.Threading.Tasks;
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
            var config = configuredOptions.Value;
            var contentRoot = config.BasePath ?? environment.ContentRootPath;
            _storageBasePath = Path.Combine(contentRoot, config.BlobsDirectoryName);
        }
        
        public Task<Stream> ReadAsync(string location)
        {
            var path = MapPath(location);
            return Task.FromResult(File.Exists(path) ? File.OpenRead(path) : (Stream)null);
        }

        public Task DeleteAsync(string location)
        {
            var path = MapPath(location);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Task.CompletedTask;
        }

        public async Task<string> SaveAsync(string repoName, string digest, Stream stream)
        {
            var location = Path.Combine(repoName, digest.Replace(':', Path.DirectorySeparatorChar));
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

            await using var outputStream = File.Create(savePath);
            await stream.CopyToAsync(outputStream);
            
            return savePath.Substring(_storageBasePath.Length).Trim(Path.DirectorySeparatorChar);
        }

        public bool SupportsDownloading { get; } = false;
        public Task<string> GenerateDownloadUrlAsync(string location)
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