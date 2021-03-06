using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Registry;
using SharpCR.Registry.Records;

namespace SharpCR.Features.LocalStorage
{
    public class DiskRecordStore: IRecordStore
    {
        private readonly LocalStorageConfiguration _config;
        private HashSet<ArtifactRecord> _allArtifacts;
        private HashSet<BlobRecord> _allBlobs;
        private Dictionary<string, List<ArtifactRecord>> _allRecordsByRepo;
        private Dictionary<string, List<BlobRecord>> _allBlobsByRepo;
        private readonly ReaderWriterLock _locker = new ReaderWriterLock();
        private static readonly TimeSpan LockerTimeout = TimeSpan.FromSeconds(3);

        public DiskRecordStore(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            _config = configuredOptions.Value;
            _config.BasePath ??= environment.ContentRootPath;
            
            ReadFromFile();
        }

        T ReadResource<T>(Func<T> readOperation)
        {
            try
            {
                _locker.AcquireReaderLock(LockerTimeout);
                return readOperation();
            }
            finally
            {
                _locker.ReleaseReaderLock();
            }
        }

        void WriteResource(Action writeOperation, bool writeFile = true)
        {
            try
            {
                _locker.AcquireWriterLock(LockerTimeout);
                writeOperation();
                RecordsUpdated(writeFile);
            }
            finally
            {
                _locker.ReleaseWriterLock();
            }
        }

        public IQueryable<ArtifactRecord> ListArtifact(string repoName)
        {
            return ReadResource(() => _allArtifacts.AsQueryable());
        }

        public ArtifactRecord GetArtifactByTag(string repoName, string tag)
        {
            return ReadResource(() =>
            {
                return _allRecordsByRepo.TryGetValue(repoName, out var repoArtifacts)
                        ? repoArtifacts.FirstOrDefault(a => string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase))
                        : null;
            });
        }

        public ArtifactRecord GetArtifactByDigest(string repoName, string digestString)
        {
            return ReadResource(() =>
            {
                return _allRecordsByRepo.TryGetValue(repoName, out var repoArtifacts)
                    ? repoArtifacts.FirstOrDefault(a =>
                        string.Equals(a.DigestString, digestString, StringComparison.OrdinalIgnoreCase))
                    : null;
            });
        }

        public void DeleteArtifact(ArtifactRecord artifactRecord)
        {
            var actualItem = GetArtifactByDigest(artifactRecord.RepositoryName, artifactRecord.DigestString);
            if (actualItem != null)
            {
                WriteResource(() =>
                {
                    _allArtifacts.Remove(actualItem);
                });
            }
        }

        public void UpdateArtifact(ArtifactRecord artifactRecord)
        {
            var actualItem = GetArtifactByDigest(artifactRecord.RepositoryName, artifactRecord.DigestString);
            if (actualItem != null)
            {
                WriteResource(() =>
                {
                    _allArtifacts.Remove(actualItem);
                    _allArtifacts.Add(artifactRecord);
                });
            }
        }

        public void CreateArtifact(ArtifactRecord artifactRecord)
        {
            WriteResource(() =>
            {
                _allArtifacts.Add(artifactRecord);
            });
        }

        public BlobRecord GetBlobByDigest(string repoName, string digest)
        {
            return ReadResource(() =>
            {
                return _allBlobsByRepo.TryGetValue(repoName, out var repoBlobs)
                    ? repoBlobs.FirstOrDefault(a =>
                        string.Equals(a.DigestString, digest, StringComparison.OrdinalIgnoreCase))
                    : null;
            });
        }

        public void DeleteBlob(BlobRecord blobRecord)
        {
            var actualItem = GetBlobByDigest(blobRecord.RepositoryName, blobRecord.DigestString);
            if (actualItem != null)
            {
                WriteResource(() =>
                {
                    _allBlobs.Remove(actualItem);
                });
            }
        }

        public void CreateBlob(BlobRecord blobRecord)
        {
            WriteResource(() =>
            {
                _allBlobs.Add(blobRecord);
            });
        }

        private void ReadFromFile()
        {
            WriteResource(() =>
            {
                var dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
                if (!File.Exists(dataFile))
                {
                    _allArtifacts = new HashSet<ArtifactRecord>();
                    _allBlobs = new HashSet<BlobRecord>();
                    return;
                }

                using var fs = File.OpenRead(dataFile);
                var bytes = File.ReadAllBytes(dataFile);
                var storedObject = JsonSerializer.Deserialize<DataObjects>(bytes,
                    new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                _allArtifacts = storedObject.Artifacts.ToHashSet();
                _allBlobs = storedObject.Blobs.ToHashSet();
            }, false);
        }

        private void RecordsUpdated(bool writeFile)
        {
            SyncIndexes();
            if (!writeFile)
            {
                return;
            }

            Task.Run(() =>
            {
                WriteResource(() =>
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
                });
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
