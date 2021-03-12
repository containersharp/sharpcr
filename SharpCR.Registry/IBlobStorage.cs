using System.IO;
using System.Threading.Tasks;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {

        Task<Stream> ReadAsync(string location);
        Task<bool> ExistAsync(string location);
        Task DeleteAsync(string location);
        Task<string> SaveAsync(string repoName, string digest, Stream stream);

        bool SupportsDownloading { get; }
        Task<string> GenerateDownloadUrlAsync(string location);
    }
}
