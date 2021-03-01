using System.IO;

namespace SharpCR.Registry
{
    public interface IBlobStorage
    {
        Stream GetByDigest(string url);

        void DeleteByDigest(string url);

        string Save(string repoName, string digest, Stream stream);
    }
}
