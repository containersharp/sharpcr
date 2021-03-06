using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();


        public Stream Read(string location)
        {
            return _blobs.ContainsKey(location) ? new MemoryStream(_blobs[location]) : null;
        }

        public void Delete(string location)
        {
            _blobs.Remove(location);
        }


        public bool SupportsDownloading { get; } = false;

        public string GenerateDownloadUrl(string location)
        {
            throw new System.NotImplementedException();
        }

        public string Save(string repoName, string digest, Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            
            var key = Guid.NewGuid().ToString();
            _blobs.Add(key, ms.ToArray());
            return key;
        }

        public List<byte[]> GetStoredBlobs()
        {
            return _blobs.Values.ToList();
        }
    }
}
