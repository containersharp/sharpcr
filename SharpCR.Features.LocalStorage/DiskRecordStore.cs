using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Features.Records;

namespace SharpCR.Features.LocalStorage
{
    public class DiskRecordStore: IRecordStore, IDisposable
    {
        private readonly LocalStorageConfiguration _config;
        private volatile ConcurrentDictionary<string, ConcurrentDictionary<ArtifactRecord, object>> _allArtifactsByRepo;
        private volatile ConcurrentDictionary<string, ConcurrentDictionary<BlobRecord, object>> _allBlobsByRepo;
        private int _pendingWriting = 0;

        public DiskRecordStore(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            _config = configuredOptions.Value;
            _config.BasePath ??= environment.ContentRootPath;
            
            ReadFromFile();
        }

        public IQueryable<ArtifactRecord> QueryArtifacts(string repoName)
        {
            if (_allArtifactsByRepo.TryGetValue(repoName, out var items))
            {
                return items.Keys.AsQueryable();
            }
            return new ArtifactRecord[0].AsQueryable(); 
        }

        public Task<ArtifactRecord> GetArtifactByTagAsync(string repoName, string tag)
        {
            var artifactRecord = _allArtifactsByRepo.TryGetValue(repoName, out var repoArtifacts)
                ? repoArtifacts.Keys.FirstOrDefault(a => string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase))
                : null;
            return Task.FromResult(artifactRecord);
        }

        public Task<ArtifactRecord[]> GetArtifactsByDigestAsync(string repoName, string digestString)
        {
            var artifactRecord =  _allArtifactsByRepo.TryGetValue(repoName, out var artifactsInRepo)
                ? artifactsInRepo.Keys.Where(a => string.Equals(a.DigestString, digestString, StringComparison.OrdinalIgnoreCase)).ToArray()
                : new ArtifactRecord[0];
            return Task.FromResult(artifactRecord);
        }

        public Task DeleteArtifactAsync(ArtifactRecord artifactRecord)
        {
            var artifactsInRepo = QueryArtifacts(artifactRecord.RepositoryName);
            var actualItem = artifactsInRepo.FirstOrDefault(a =>
                string.Equals(a.DigestString, artifactRecord.DigestString, StringComparison.OrdinalIgnoreCase)
                && (artifactRecord.Tag == null || string.Equals(a.Tag, artifactRecord.Tag, StringComparison.OrdinalIgnoreCase)));

            if (actualItem != null && _allArtifactsByRepo.TryGetValue(artifactRecord.RepositoryName, out var repoArtifacts))
            {
                repoArtifacts.TryRemove(actualItem, out _);
                RecordsUpdated();
            }
            
            return Task.CompletedTask;
        }

        public Task UpdateArtifactAsync(ArtifactRecord artifactRecord)
        {
            var artifactsInRepo = QueryArtifacts(artifactRecord.RepositoryName);
            var actualItem = artifactsInRepo.FirstOrDefault(a =>
                string.Equals(a.DigestString, artifactRecord.DigestString, StringComparison.OrdinalIgnoreCase)
                && (artifactRecord.Tag == null || string.Equals(a.Tag, artifactRecord.Tag, StringComparison.OrdinalIgnoreCase)));
            
            if (actualItem != null && _allArtifactsByRepo.TryGetValue(artifactRecord.RepositoryName, out var repoArtifacts))
            {
                repoArtifacts.TryRemove(actualItem, out _);
                repoArtifacts.TryAdd(artifactRecord, null /* we don't need this value */);
                RecordsUpdated();
            }

            return Task.CompletedTask;
        }

        public Task CreateArtifactAsync(ArtifactRecord artifactRecord)
        {
            if (!_allArtifactsByRepo.TryGetValue(artifactRecord.RepositoryName, out var repoArtifacts))
            {
                repoArtifacts = new ConcurrentDictionary<ArtifactRecord, object>();
                _allArtifactsByRepo.TryAdd(artifactRecord.RepositoryName, repoArtifacts);
            }
            
            if (_allArtifactsByRepo.TryGetValue(artifactRecord.RepositoryName, out var repoArtifacts2))
            {
                repoArtifacts2.TryAdd(artifactRecord, null /* we don't need this value */);
            }
            
            RecordsUpdated();
            return Task.CompletedTask;
        }

        public Task<BlobRecord> GetBlobByDigestAsync(string repoName, string digest)
        {
            var blobRecord = _allBlobsByRepo.TryGetValue(repoName, out var repoBlobs)
                ? repoBlobs.Keys.FirstOrDefault(a => string.Equals(a.DigestString, digest, StringComparison.OrdinalIgnoreCase))
                : null;
            return Task.FromResult(blobRecord);
        }

        public async Task DeleteBlobAsync(BlobRecord blobRecord)
        {
            var actualItem = await GetBlobByDigestAsync(blobRecord.RepositoryName, blobRecord.DigestString);
            if (_allBlobsByRepo.TryGetValue(blobRecord.RepositoryName, out var blobsInRepo))
            {
                blobsInRepo.TryRemove(actualItem, out _);
                RecordsUpdated();
            }
        }

        public Task CreateBlobAsync(BlobRecord blobRecord)
        {
            if (!_allBlobsByRepo.TryGetValue(blobRecord.RepositoryName, out var blobsInRepo))
            {
                blobsInRepo = new ConcurrentDictionary<BlobRecord, object>();
                _allBlobsByRepo.TryAdd(blobRecord.RepositoryName, blobsInRepo);
            }
            
            if (_allBlobsByRepo.TryGetValue(blobRecord.RepositoryName, out var blobsInRepo2))
            {
                blobsInRepo2.TryAdd(blobRecord, null /* we don't need this value */);
            }
            
            RecordsUpdated();
            return Task.CompletedTask;
        }

        private void ReadFromFile()
        {
            var dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
            var artifactList = new List<ArtifactRecord>();
            var blobList = new List<BlobRecord>();
            if (File.Exists(dataFile))
            {
                using var fs = File.OpenRead(dataFile);
                var valueTask = JsonSerializer.DeserializeAsync<DataObjects>(fs, 
                    new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase})
                    .ConfigureAwait(false);
                var storedObject = valueTask.GetAwaiter().GetResult();
                artifactList = storedObject.Artifacts.ToList();
                blobList = storedObject.Blobs.ToList();
            }
            
            _allArtifactsByRepo = new ConcurrentDictionary<string, ConcurrentDictionary<ArtifactRecord, object>>(
                artifactList.GroupBy(a => a.RepositoryName)
                    .ToDictionary(g => g.Key,
                        g => new ConcurrentDictionary<ArtifactRecord, object>(
                            g.ToDictionary(x => x, x => (object)null)
                        )));
            _allBlobsByRepo = new ConcurrentDictionary<string, ConcurrentDictionary<BlobRecord, object>>(
                blobList.GroupBy(b => b.RepositoryName)
                    .ToDictionary(g => g.Key,
                    g => new ConcurrentDictionary<BlobRecord, object>(
                        g.ToDictionary(x => x, x => (object)null)
                        )));
        }

        private void WriteToFile()
        {
            var dataObjects = new DataObjects
            {
                Artifacts = _allArtifactsByRepo.Values.Select(x => x.Keys).SelectMany(x => x) .ToArray(),
                Blobs =  _allBlobsByRepo.Values.Select(x => x.Keys).SelectMany(x => x).ToArray()
            };

            var dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
            if (!Directory.Exists(_config.BasePath))
            {
                Directory.CreateDirectory(_config.BasePath);
            }

            using var fs = File.OpenWrite(dataFile);
            using var utf8JsonWriter = new Utf8JsonWriter(fs);
            JsonSerializer.Serialize(utf8JsonWriter, dataObjects, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        }

        private void RecordsUpdated()
        {
            if (_pendingWriting > 0)
            {
                return;
            }

            Interlocked.Increment(ref _pendingWriting);
            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false).GetAwaiter().GetResult();
                WriteToFileNow();
            });
        }

        private void WriteToFileNow()
        {
            if (_pendingWriting == 0)
            {
                return;
            }
            
            WriteToFile();
            Interlocked.Decrement(ref _pendingWriting);
        }

        public void Dispose()
        {
            WriteToFileNow();
        }
    }

    public class DataObjects
    {
        public ArtifactRecord[] Artifacts { get; set; }
        public BlobRecord[] Blobs { get; set; }

    }
}
