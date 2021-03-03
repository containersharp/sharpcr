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
        private HashSet<ArtifactRecord> _allRecords;
        private Dictionary<string, List<ArtifactRecord>> _allRecordsByRepo;
        private readonly ReaderWriterLock _locker = new ReaderWriterLock();
        private static readonly TimeSpan LockerTimeout = TimeSpan.FromMilliseconds(50);

        public DiskRecordStore(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            _config = configuredOptions.Value ?? new LocalStorageConfiguration();
            _config.BasePath ??= environment.WebRootPath;
            
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

        void WriteResource(Action writeOperation)
        {
            try
            {
                _locker.AcquireWriterLock(LockerTimeout);
                writeOperation();
            }
            finally
            {
                _locker.ReleaseWriterLock();
            }
        }

        public IQueryable<ArtifactRecord> ListArtifact(string repoName)
        {
            return ReadResource(() => _allRecords.AsQueryable());
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
                    _allRecords.Remove(actualItem);
                    ArtifactsUpdated();
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
                    _allRecords.Remove(actualItem);
                    _allRecords.Add(artifactRecord);
                    ArtifactsUpdated();
                });
            }
        }

        public void CreateArtifact(ArtifactRecord artifactRecord)
        {
            WriteResource(() =>
            {
                _allRecords.Add(artifactRecord);
                ArtifactsUpdated();
            });
        }

        public BlobRecord GetBlobByDigest(string repoName, string digest)
        {
            throw new NotImplementedException();
        }

        public void DeleteBlob(BlobRecord blobRecord)
        {
            throw new NotImplementedException();
        }

        public void CreateBlob(BlobRecord blobRecord)
        {
            throw new NotImplementedException();
        }


        private void ReadFromFile()
        {
            var  dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
            if (!File.Exists(dataFile))
            {
                _allRecords = new HashSet<ArtifactRecord>();
                return;
            }

            using var fs = File.OpenRead(dataFile);
            var bytes = File.ReadAllBytes(dataFile);
            var storedObject = JsonSerializer.Deserialize<DataObjects>(bytes, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            _allRecords = storedObject.Artifacts.ToHashSet();
        }

        private void ArtifactsUpdated()
        {
            _allRecordsByRepo = _allRecords
                .GroupBy(a => a.RepositoryName)
                .ToDictionary(g => g.Key,
                    g=> g.ToList());

            Task.Run(() =>
            {
                WriteResource(() =>
                {
                    var dataObjects = new DataObjects
                    {
                        Artifacts = _allRecords.ToArray()
                    };

                    var dataFile = Path.Combine(_config.BasePath, _config.RecordsFileName);
                    using var fs = File.OpenWrite(dataFile);
                    using var utf8JsonWriter = new Utf8JsonWriter(fs);
                    JsonSerializer.Serialize(utf8JsonWriter, dataObjects, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                });
            });
        }
    }

    public class DataObjects
    {
        public ArtifactRecord[] Artifacts { get; set; }

    }
}
