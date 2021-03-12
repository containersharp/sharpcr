using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();


        public Task<Stream> ReadAsync(string location)
        {
            return Task.FromResult(_blobs.ContainsKey(location) ? new MemoryStream(_blobs[location]) : (Stream)null);
        }

        public Task<bool> ExistAsync(string location)
        {
            return Task.FromResult(_blobs.ContainsKey(location));
        }

        public Task DeleteAsync(string location)
        {
            if(_blobs.ContainsKey(location))
                _blobs.Remove(location);
            
            return Task.CompletedTask;
        }


        public bool SupportsDownloading { get; } = false;

        public Task<string> GenerateDownloadUrlAsync(string location)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> SaveAsync(string repoName, string digest, Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            
            var key = Guid.NewGuid().ToString();
            _blobs.Add(key, ms.ToArray());
            return Task.FromResult(key);
        }

        public List<byte[]> GetStoredBlobs()
        {
            return _blobs.Values.ToList();
        }
    }
}
