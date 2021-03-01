using System.Collections.Generic;
using System.IO;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, Stream> _blobs = new Dictionary<string, Stream>();

        public Stream GetByDigest(string url)
        {
            if (_blobs.ContainsKey(url))
            {
                return _blobs[url];
            }

            return null;
        }

        public void DeleteByDigest(string url)
        {
            _blobs.Remove(url);
        }

        public string Save(string repoName, string digest, Stream stream)
        {
            var key = getKey(repoName, digest);

            _blobs.Add(key, stream);

            return key;
        }

        private string getKey(string repoName, string digest)
        {
            return digest + "@" + digest;
        }

    }
}
