using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace SharpCR.Features.LocalStorage
{
    public class DiskBlobStorage : IBlobStorage
    {
        private readonly string _storageBasePath;
        private readonly FileIndexer _blobIndexer;

        public DiskBlobStorage(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            var config = configuredOptions.Value;
            var contentRoot = config.BasePath ?? environment.ContentRootPath;
            _storageBasePath = Path.Combine(contentRoot, config.BlobsDirectoryName);
            _blobIndexer = new FileIndexer(Path.Combine(_storageBasePath, "index.txt"));
        }

        public async Task<string> TryLocateExistingAsync(string digest)
        {
            return await _blobIndexer.TryGetLocation(digest);
        }

        public Task<Stream> ReadAsync(string location)
        {
            var path = MapPath(location);
            return Task.FromResult(File.Exists(path) ? File.OpenRead(path) : (Stream)null);
        }

        public async Task DeleteAsync(string location)
        {
            var path = MapPath(location);
            if (File.Exists(path))
            {
                File.Delete(path);
                await _blobIndexer.RemoveAsync(location);
            }
        }

        public async Task<string> SaveAsync(string digest, Stream stream, string repoName)
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
            
            var saveLocation = savePath.Substring(_storageBasePath.Length).Trim(Path.DirectorySeparatorChar);
            await _blobIndexer.AddAsync(digest, saveLocation);
            return saveLocation;
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

        public class FileIndexer
        {
            private readonly string _indexFilePath;
            const string Splitter = "$";

            public FileIndexer(string indexFilePath)
            {
                _indexFilePath = indexFilePath;
                var dir = Path.GetDirectoryName(indexFilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(indexFilePath))
                {
                    File.Create(indexFilePath).Dispose();
                }
            }
            public async Task AddAsync(string digest, string location)
            {
                await using var fs = File.OpenWrite(_indexFilePath);
                await using var writer = new StreamWriter(fs);
                await writer.WriteLineAsync($"{digest}{Splitter}{location}");
            }

            public async Task RemoveAsync(string location)
            {
                var lineSuffix = $"{Splitter}{location}";
                var newIndexFile = _indexFilePath + ".tmp";

                var fs = File.OpenRead(_indexFilePath);
                var fsOut = File.OpenWrite(newIndexFile);

                using var reader = new StreamReader(fs);
                using var writer = new StreamWriter(fsOut);
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        break;
                    }

                    if (!line.EndsWith(lineSuffix))
                    {
                        await writer.WriteLineAsync(line);
                    }
                    else
                    {
                        await writer.FlushAsync();
                        await fs.CopyToAsync(fsOut);
                        break;
                    }
                }

                await writer.DisposeAsync();
                await fsOut.DisposeAsync();
                await fs.DisposeAsync();
                File.Move(newIndexFile, _indexFilePath, true);
            }

            public async Task<string> TryGetLocation(string digest)
            {
                var linePrefix = $"{digest}{Splitter}";
                
                await using var fs = File.OpenRead(_indexFilePath);
                using var reader = new StreamReader(fs);
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith(linePrefix))
                    {
                        return line.Substring(linePrefix.Length);
                    }
                }

                return null;
            }
            
            // todo: test this indexer
        }
    }
}