using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace SharpCR.Features.LocalStorage
{
    public class DiskBlobStorage : IBlobStorage, IDisposable
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
            var location = await _blobIndexer.TryGetLocation(digest);
            if (location == null)
            {
                return null;
            }

            if (File.Exists(MapPath(location)))
            {
                return location;
            }
            
            // We don't delete index right after a blob is deleted (that will need to scan the whole index).
            // So we postpone this removing to next locating when we find this file is actually missing from the disk.
            var _ = _blobIndexer.RemoveAsync(digest, location);
            return null;
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

        public async Task<string> SaveAsync(FileInfo temporaryFile, string repoName, string digest)
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


            await using var inputStream = temporaryFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var outputStream = File.Create(savePath);
            await inputStream.CopyToAsync(outputStream);
            
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

        private class FileIndexer : IDisposable
        {
            private readonly string _indexFilePath;
            private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
            private readonly ConcurrentDictionary<string, string> _cachedItems = new ConcurrentDictionary<string, string>();
            private FileStream _indexFileStream;
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
                _indexFileStream =  File.Open(indexFilePath, FileMode.Open, FileAccess.ReadWrite);
            }
            
            public async Task AddAsync(string digest, string location)
            {
                _cachedItems.TryAdd(digest, location);
            
                try
                {
                    await _semaphoreSlim.WaitAsync();

                    _indexFileStream.Seek(0, SeekOrigin.End);
                    var writer = new StreamWriter(_indexFileStream);
                    await writer.WriteLineAsync($"{digest}{Splitter}{location}");
                    await writer.FlushAsync();
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }

            public async Task RemoveAsync(string digest, string location)
            {
                _cachedItems.TryRemove(digest, out _);
                
                try
                {
                    await _semaphoreSlim.WaitAsync();
                    
                    var lineMatch = $"{digest}{Splitter}{location}";
                    var newIndexFile = _indexFilePath + ".tmp";

                    var fsOut = File.OpenWrite(newIndexFile);
                    _indexFileStream.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(_indexFileStream);
                    var writer = new StreamWriter(fsOut);
                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            break;
                        }

                        if (!line.Equals(lineMatch, StringComparison.Ordinal))
                        {
                            await writer.WriteLineAsync(line);
                        }
                        else
                        {
                            await writer.FlushAsync();
                            await _indexFileStream.CopyToAsync(fsOut);
                            break;
                        }
                    }

                    await writer.FlushAsync();
                    await writer.DisposeAsync();
                    await fsOut.DisposeAsync();
                    await _indexFileStream.DisposeAsync();
                    File.Move(newIndexFile, _indexFilePath, true);
                    _indexFileStream = File.Open(_indexFilePath, FileMode.Open, FileAccess.ReadWrite);
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }

            public async Task<string> TryGetLocation(string digest)
            {
                if (_cachedItems.TryGetValue(digest, out var location))
                {
                    return location;
                }
                
                try
                {
                    await _semaphoreSlim.WaitAsync();

                    string foundLocation = null;
                    var linePrefix = $"{digest}{Splitter}";

                    _indexFileStream.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(_indexFileStream);
                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            break;
                        }

                        var values = line.Split(Splitter, StringSplitOptions.RemoveEmptyEntries);
                        _cachedItems.AddOrUpdate(values[0], values[1], (key, oldValue) => values[1]);
                        if (line.StartsWith(linePrefix))
                        {
                            foundLocation = line.Substring(linePrefix.Length);
                            break;
                        }
                    }

                    return foundLocation;
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
            
            public void Dispose()
            {
                _indexFileStream?.Dispose();
                _semaphoreSlim.Dispose();
            }
        }

        public void Dispose()
        {
            _blobIndexer?.Dispose();
        }
    }
}