using System.Linq;
using System.Threading.Tasks;
using SharpCR.Features.Records;

namespace SharpCR.Features
{
    public interface IRecordStore
    {
        IQueryable<ArtifactRecord> QueryArtifacts(string repoName);

        Task<ArtifactRecord> GetArtifactByTagAsync(string repoName, string tag);

        Task<ArtifactRecord[]> GetArtifactsByDigestAsync(string repoName, string digestString);

        Task DeleteArtifactAsync(ArtifactRecord artifactRecord);
        Task UpdateArtifactAsync(ArtifactRecord artifactRecord);
        Task CreateArtifactAsync(ArtifactRecord artifactRecord);

        Task<BlobRecord> GetBlobByDigestAsync(string repoName, string digest);

        Task DeleteBlobAsync(BlobRecord blobRecord);
        
        Task CreateBlobAsync(BlobRecord blobRecord);
    }
}
