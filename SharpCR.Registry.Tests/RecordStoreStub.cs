using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpCR.Registry.Records;

namespace SharpCR.Registry.Tests
{
    public class RecordStoreStub : IRecordStore
    {
        private List<ArtifactRecord> _artifacts = new List<ArtifactRecord>();
        private List<BlobRecord> _blobs = new List<BlobRecord>();

        public RecordStoreStub WithArtifacts(params ArtifactRecord[] artifacts)
        {
            _artifacts = new List<ArtifactRecord>(artifacts ?? new ArtifactRecord[0]);
            return this;
        }

        public RecordStoreStub WithBlobs(params BlobRecord[] blobs)
        {
            _blobs = new List<BlobRecord>(blobs ?? new BlobRecord[0]);
            return this;
        }

        public Task<IQueryable<ArtifactRecord>> ListArtifactAsync(string repoName)
        {
            var result= _artifacts
                .Where(artifact => string.Equals(artifact.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
            
            return Task.FromResult(result);
        }

        public Task<ArtifactRecord> GetArtifactByTagAsync(string repoName, string tag)
        {
            var record = _artifacts.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(record);
        }

        public Task<ArtifactRecord> GetArtifactByDigestAsync(string repoName, string digestString)
        {
            var record = _artifacts.FirstOrDefault(t =>
                string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.DigestString, digestString, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(record);
        }

        public Task DeleteArtifactAsync(ArtifactRecord artifactRecord)
        {
            var index = _artifacts.IndexOf(artifactRecord);
            if (index >= 0)
            {
                _artifacts.RemoveAt(index);
            }
            return Task.CompletedTask;
        }

        public async Task UpdateArtifactAsync(ArtifactRecord artifactRecord)
        {
            await DeleteArtifactAsync(artifactRecord);
            await CreateArtifactAsync(artifactRecord);
        }

        public Task CreateArtifactAsync(ArtifactRecord artifactRecord)
        {
            _artifacts.Add(artifactRecord);
            return Task.CompletedTask;
        }

        public Task<BlobRecord> GetBlobByDigestAsync(string repoName, string digest)
        {
            var record = _blobs.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.DigestString, digest, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(record);
        }

        public Task DeleteBlobAsync(BlobRecord blobRecord)
        {
            var index = _blobs.IndexOf(blobRecord);
            if (index >= 0)
            {
                _blobs.RemoveAt(index);
            }
            return Task.CompletedTask;
        }

        public Task CreateBlobAsync(BlobRecord blobRecord)
        {
            _blobs.Add(blobRecord);
            return Task.CompletedTask;
        }
    }
}
