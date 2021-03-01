using System;
using System.Collections.Generic;
using System.IO;

namespace SharpCR.Registry.Tests
{
    public class BlobStorageStub : IBlobStorage
    {
        private readonly Dictionary<string, Stream> _blobs = new Dictionary<string, Stream>();

        public Stream Get(string location)
        {
            if (_blobs.ContainsKey(location))
            {
                return _blobs[location];
            }

            return null;
        }

        public void Delete(string location)
        {
            _blobs.Remove(location);
        }

        public string Save(Stream stream)
        {
            var location = Guid.NewGuid().ToString();

            _blobs.Add(location, stream);

            return location;
        }
    }
}
