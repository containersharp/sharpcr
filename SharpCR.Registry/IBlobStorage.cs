namespace SharpCR.Registry
{
    public interface IBlobStorage
    {
        byte[] GetByDigest(string repoName, string digest);

        void DeleteByDigest(string repoName, string digest);

        void Save(string repoName, string digest, byte[] content);
    }
}
