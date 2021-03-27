using System;
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
    public class DiskRecordStore: IRecordStore
    {
        private readonly LocalStorageConfiguration _config;
        private HashSet<ArtifactRecord> _allArtifacts;
        private HashSet<BlobRecord> _allBlobs;
        private Dictionary<string, List<ArtifactRecord>> _allRecordsByRepo;
        private Dictionary<string, List<BlobRecord>> _allBlobsByRepo;
        private int _pendingWriting = 0;

        public DiskRecordStore(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            _config = configuredOptions.Value;
            _config.BasePath ??= environment.ContentRootPath;
            
            ReadFromFile();
        }

        public Task<IQueryable<ArtifactRecord>> ListArtifactAsync(string repoName)
        {
            return Task.FromResult(_allArtifacts.AsQueryable());
        }

        public Task<ArtifactRecord> GetArtifactByTagAsync(string repoName, string tag)
        {
            var artifactRecord = _allRecordsByRepo.TryGetValue(repoName, out var repoArtifacts)
                ? repoArtifacts.FirstOrDefault(a => string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase))
                : null;
            return Task.FromResult(artifactRecord);
        }

        public Task<ArtifactRecord> GetArtifactByDigestAsync(string repoName, string digestString)
        {
            var artifactRecord =  _allRecordsByRepo.TryGetValue(repoName, out var repoArtifacts)
                ? repoArtifacts.FirstOrDefault(a => string.Equals(a.DigestString, digestString, StringComparison.OrdinalIgnoreCase))
                : null;
            return Task.FromResult(artifactRecord);
        }

        public async Task DeleteArtifactAsync(ArtifactRecord artifactRecord)
        {
            var actualItem = await GetArtifactByDigestAsync(artifactRecord.RepositoryName, artifactRecord.DigestString);
            
            _allArtifacts.Remove(actualItem);
            RecordsUpdated(true);
        }

        public async Task UpdateArtifactAsync(ArtifactRecord artifactRecord)
        {
            var actualItem = await GetArtifactByDigestAsync(artifactRecord.RepositoryName, artifactRecord.DigestString);
            if (actualItem != null)
            {
                _allArtifacts.Remove(actualItem);
                _allArtifacts.Add(artifactRecord);
                RecordsUpdated(true);
            }
        }

        public Task CreateArtifactAsync(ArtifactRecord artifactRecord)
        {
            _allArtifacts.Add(artifactRecord);
            RecordsUpdated(true);
            return Task.CompletedTask;
        }

        public Task<BlobRecord> GetBlobByDigestAsync(string repoName, string digest)
        {
            var blobRecord = _allBlobsByRepo.TryGetValue(repoName, out var repoBlobs)
                ? repoBlobs.FirstOrDefault(a => string.Equals(a.DigestString, digest, StringComparison.OrdinalIgnoreCase))
                : null;
            return Task.FromResult(blobRecord);
        }

        public async Task DeleteBlobAsync(BlobRecord blobRecord)
        {
            var actualItem = await GetBlobByDigestAsync(blobRecord.RepositoryName, blobRecord.DigestString);
            if (actualItem != null)
            {
                _allBlobs.Remove(actualItem);
                RecordsUpdated(true);
            }
        }

        public Task CreateBlobAsync(BlobRecord blobRecord)
        {
            _allBlobs.Add(blobRecord);
            RecordsUpdated(true);
            return Task.CompletedTask;
        }

        private void ReadFromFile()
        {
            var dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
            if (!File.Exists(dataFile))
            {
                _allArtifacts = new HashSet<ArtifactRecord>();
                _allBlobs = new HashSet<BlobRecord>();
                SyncIndexes();
                return;
            }

            using var fs = File.OpenRead(dataFile);
            var bytes = File.ReadAllBytes(dataFile);
            var storedObject = JsonSerializer.Deserialize<DataObjects>(bytes,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            _allArtifacts = storedObject.Artifacts.ToHashSet();
            _allBlobs = storedObject.Blobs.ToHashSet();
            SyncIndexes();
        }

        private void WriteToFile()
        {
            var dataObjects = new DataObjects
            {
                Artifacts = _allArtifacts.ToArray(),
                Blobs = _allBlobs.ToArray()
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

        private void RecordsUpdated(bool writeFile)
        {
            SyncIndexes();
            if (!writeFile)
            {
                return;
            }

            if (_pendingWriting > 0)
            {
                return;
            }

            Interlocked.Increment(ref _pendingWriting);
            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                WriteToFile();
                Interlocked.Decrement(ref _pendingWriting);
            });
        }
        
        

        private void SyncIndexes()
        {
            _allRecordsByRepo = _allArtifacts
                .GroupBy(a => a.RepositoryName)
                .ToDictionary(g => g.Key,
                    g => g.ToList());
            
            _allBlobsByRepo = _allBlobs
                .GroupBy(a => a.RepositoryName)
                .ToDictionary(g => g.Key,
                    g => g.ToList());
        }
    }

    public class DataObjects
    {
        public ArtifactRecord[] Artifacts { get; set; }
        public BlobRecord[] Blobs { get; set; }

    }
}
