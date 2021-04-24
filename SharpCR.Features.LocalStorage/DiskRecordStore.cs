using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Features.Records;

namespace SharpCR.Features.LocalStorage
{
    public class DiskRecordStore: IRecordStore, IDisposable
    {
        private readonly LiteDatabase _db;

        public DiskRecordStore(IWebHostEnvironment environment, IOptions<LocalStorageConfiguration> configuredOptions)
        {
            var config = configuredOptions.Value;
            config.BasePath ??= environment.ContentRootPath;
            
            // Open database (or create if doesn't exist)
            if (!Directory.Exists(config.BasePath))
            {
                Directory.CreateDirectory(config.BasePath);
            }
            _db = new LiteDatabase(Path.Combine(config.BasePath, config.RecordsFileName));
        }

        public Task<IEnumerable<string>> GetTags(string repoName)
        {
            var artifacts = GetArtifactsCollection();
            var tags = artifacts.Find(a =>
                    a.Tag != null && repoName != null && a.RepositoryName.ToLower() == repoName.ToLower())
                .Select(a => a.Tag)
                .ToList();

            return Task.FromResult((IEnumerable<string>)tags);
        }

        public Task<ArtifactRecord> GetArtifactByTagAsync(string repoName, string tag)
        {
            var artifactRecord = GetArtifactsCollection().FindOne(a => tag != null && a.Tag.ToLower() == tag.ToLower());

            return Task.FromResult((ArtifactRecord) artifactRecord);
        }

        public Task<ArtifactRecord[]> GetArtifactsByDigestAsync(string repoName, string digestString)
        {
            var artifactRecords = GetArtifactsCollection()
                .Find(a => digestString != null && a.DigestString.ToLower() == digestString.ToLower())
                .ToArray()
                .Cast<ArtifactRecord>()
                .ToArray();

            return Task.FromResult(artifactRecords);
        }

        public Task DeleteArtifactAsync(ArtifactRecord artifactRecord)
        {
            var artifacts = GetArtifactsCollection();
            var doc = FindExistingArtifactDoc(artifacts, artifactRecord);
            
            if (doc != null)
            {
                artifacts.Delete(doc["_id"]);
            }
            return Task.CompletedTask;
        }

        public Task UpdateArtifactAsync(ArtifactRecord artifactRecord)
        {
            _db.GetCollection<ArtifactDoc>("artifacts").Update(artifactRecord as ArtifactDoc);
            return Task.CompletedTask;
        }

        public Task CreateArtifactAsync(ArtifactRecord artifactRecord)
        {
            var doc = new ArtifactDoc(artifactRecord);
            GetArtifactsCollection().Insert(doc);
            return Task.CompletedTask;
        }

        private ILiteCollection<ArtifactDoc> GetArtifactsCollection()
        {
            var artifacts = _db.GetCollection<ArtifactDoc>("artifacts");
            return artifacts;
        }


        private BsonDocument FindExistingArtifactDoc(ILiteCollection<ArtifactDoc> artifacts, ArtifactRecord artifactRecord)
        {
            var digestString = artifactRecord.DigestString;
            var tagString = artifactRecord.Tag;
            
            var actualItem = artifacts.FindOne(a =>
                    (digestString != null && a.DigestString.ToLower() == digestString.ToLower())
                    && (
                        (tagString == null && a.Tag == null) 
                        || (a.Tag.ToLower() == tagString.ToLower())
                       )
                );

            return actualItem == null ? null : _db.Mapper.ToDocument(actualItem);
        }

        
        
        
        public Task<BlobRecord> GetBlobByDigestAsync(string repoName, string digest)
        {
            var foundBlob = GetBlobCollection().FindOne(b => digest != null && b.DigestString.ToLower() == digest.ToLower());
            return Task.FromResult((BlobRecord) foundBlob);
        }

        public Task DeleteBlobAsync(BlobRecord blobRecord)
        {
            var blobs = GetBlobCollection();
            var doc = FindExistingBlobDoc(blobs, blobRecord);
            
            if (doc != null)
            {
                blobs.Delete(doc["_id"]);
            }

            return Task.CompletedTask;
        }

        public Task CreateBlobAsync(BlobRecord blobRecord)
        {
            var doc = new BlobDoc(blobRecord);
            GetBlobCollection().Insert(doc);
            return Task.CompletedTask;
        }

        private ILiteCollection<BlobDoc> GetBlobCollection()
        {
            return _db.GetCollection<BlobDoc>("blobs");
        }


        private BsonDocument FindExistingBlobDoc(ILiteCollection<BlobDoc> artifacts, BlobRecord blob)
        {
            var digest = blob.DigestString;
            var actualItem = artifacts.FindOne(b => digest != null && b.DigestString.ToLower() == digest.ToLower());
            return actualItem == null ? null : _db.Mapper.ToDocument(actualItem);
        }


        public void Dispose()
        {
            _db.Dispose();
        }
    }

    public class ArtifactDoc: ArtifactRecord
    {
        public ArtifactDoc()
        {
            
        }

        public ArtifactDoc(ArtifactRecord artifactRecord)
        {
            this.Tag = artifactRecord.Tag;
            this.DigestString = artifactRecord.DigestString;
            this.ManifestBytes = artifactRecord.ManifestBytes;
            this.RepositoryName = artifactRecord.RepositoryName;
            this.ManifestMediaType = artifactRecord.ManifestMediaType;
        }
        
        [BsonId]
        public ObjectId _docId { get; set; }
    }
    public class BlobDoc: BlobRecord
    {
        public BlobDoc()
        {
            
        }

        public BlobDoc(BlobRecord blobRecord)
        {
            this.ContentLength = blobRecord.ContentLength;
            this.DigestString = blobRecord.DigestString;
            this.MediaType  = blobRecord.MediaType;
            this.RepositoryName = blobRecord.RepositoryName;
            this.StorageLocation = blobRecord.StorageLocation;
        }
        
        
        [BsonId]
        public ObjectId _docId { get; set; }
    }
}
