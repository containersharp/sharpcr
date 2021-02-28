using System.Linq;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;

namespace SharpCR.Registry
{
    public interface IRecordStore
    {
        IQueryable<ArtifactRecord> ListArtifact(string repoName);

        ArtifactRecord GetArtifactByTag(string repoName, string tag);

        ArtifactRecord GetArtifactByDigest(string repoName, string digestString);

        void DeleteArtifact(ArtifactRecord artifactRecord);
        void UpdateArtifact(ArtifactRecord artifactRecord);
        void CreateArtifact(ArtifactRecord artifactRecord);

        bool BlobExists(Descriptor descriptor);

        BlobRecord GetBlobByDigest(string repoName, string digest);

        void DeleteBlob(BlobRecord blobRecord);
    }
}
