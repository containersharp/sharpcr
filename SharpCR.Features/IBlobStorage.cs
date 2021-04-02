using System.IO;
using System.Threading.Tasks;

namespace SharpCR.Features
{
    public interface IBlobStorage
    {
        Task<string> TryLocateExistingAsync(string digest);
        Task<Stream> ReadAsync(string location);
        Task DeleteAsync(string location);
        Task<string> SaveAsync(FileInfo temporaryFile, string repoName, string digest);

        bool SupportsDownloading { get; }
        Task<string> GenerateDownloadUrlAsync(string location);
    }
}
