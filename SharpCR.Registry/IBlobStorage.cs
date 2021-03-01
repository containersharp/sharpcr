using System.IO;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {

        Stream Read(string location);
        void Delete(string location);
        string Save(string repoName, string digest, Stream stream);

        bool SupportsDownloading { get; }
        string GenerateDownloadUrl(string location);
    }
}
