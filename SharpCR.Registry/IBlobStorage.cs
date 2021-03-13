using System.IO;
using System.Threading.Tasks;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {

        Task<string> TryLocateExistingAsync(string digest);
        Task<Stream> ReadAsync(string location);
        Task DeleteAsync(string location);
        Task<string> SaveAsync(string digest, Stream stream, string repoName);

        bool SupportsDownloading { get; }
        Task<string> GenerateDownloadUrlAsync(string location);
    }
}
