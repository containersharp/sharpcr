using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpCR.Features;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();


        public Task<string> TryLocateExistingAsync(string digest)
        {
            var location = _blobs.ContainsKey(digest) ? digest : null;
            return Task.FromResult(location);
        }

        public Task<Stream> ReadAsync(string location)
        {
            return Task.FromResult(_blobs.ContainsKey(location) ? new MemoryStream(_blobs[location]) : (Stream)null);
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

        public async Task<string> SaveAsync(FileInfo temporaryFile, string repoName, string digest)
        {
            await using var fs = temporaryFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var ms = new MemoryStream();
            fs.CopyTo(ms);

            _blobs.Add(digest, ms.ToArray());
            return digest;
        }

        public List<byte[]> GetStoredBlobs()
        {
            return _blobs.Values.ToList();
        }
    }
}
