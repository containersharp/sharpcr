using System;
using System.Collections.Generic;
using System.Linq;
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

        public IQueryable<ArtifactRecord> ListArtifact(string repoName)
        {
            return _artifacts
                .Where(artifact => string.Equals(artifact.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();
        }

        public ArtifactRecord GetArtifactByTag(string repoName, string tag)
        {
            return _artifacts.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        public ArtifactRecord GetArtifactByDigest(string repoName, string digestString)
        {
            return _artifacts.FirstOrDefault(t =>
                string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.DigestString, digestString, StringComparison.OrdinalIgnoreCase));

        }

        public void DeleteArtifact(ArtifactRecord artifactRecord)
        {
            var index = _artifacts.IndexOf(artifactRecord);
            if (index >= 0)
            {
                _artifacts.RemoveAt(index);
            }
        }

        public void UpdateArtifact(ArtifactRecord artifactRecord)
        {
            DeleteArtifact(artifactRecord);
            CreateArtifact(artifactRecord);
        }

        public void CreateArtifact(ArtifactRecord artifactRecord)
        {
            _artifacts.Add(artifactRecord);
        }

        public BlobRecord GetBlobByDigest(string repoName, string digest)
        {
            return _blobs.FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.DigestString, digest, StringComparison.OrdinalIgnoreCase));
        }

        public void DeleteBlob(BlobRecord blobRecord)
        {
            var index = _blobs.IndexOf(blobRecord);
            if (index >= 0)
            {
                _blobs.RemoveAt(index);
            }
        }
    }
}
