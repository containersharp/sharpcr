using System.Collections.Generic;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();

        public byte[] GetByDigest(string repoName, string digest)
        {
            var key = getKey(repoName, digest);

            if (_blobs.ContainsKey(key))
            {
                return _blobs[key];
            }

            return null;
        }

        public void DeleteByDigest(string repoName, string digest)
        {
            _blobs.Remove(getKey(repoName, digest));
        }

        public void Save(string repoName, string digest, byte[] content)
        {
            _blobs.Add(getKey(repoName, digest), content);
        }

        private string getKey(string repoName, string digest)
        {
            return digest + "@" + digest;
        }
    }
}
