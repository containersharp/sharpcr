using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using SharpCR.Features.LocalStorage;
using Xunit;

namespace SharpCR.Registry.Tests.Features.LocalStorage
{
    public class BlobStorageFacts
    {
        [Fact]
        public void ShouldSaveBlob()
        {
            var storage = CreateBlobStorage(out var blobsPath);

            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            using var ms = new MemoryStream(bytes);
            var location = storage.Save("abc/foo", "sha256@ab123de", ms);

            var actualPath = Path.Combine(blobsPath, location);
            Assert.True(File.Exists(actualPath));
            Assert.True(File.ReadAllBytes(actualPath).SequenceEqual(bytes));
        }
        
        [Fact]
        public void ShouldReadByLocation()
        {
            var storage = CreateBlobStorage(out _);

            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            using var ms = new MemoryStream(bytes);
            var location = storage.Save("abc/foo", "sha256@ab123de", ms);

            using var readResult = storage.Read(location);
            using var readMs = new MemoryStream();
            readResult.CopyTo(readMs);
            var readBytes = readMs.ToArray();
            
            Assert.True(readBytes.SequenceEqual(bytes));
        }


        [Fact]
        public void ShouldNotSupportDownloads()
        {
            var storage = CreateBlobStorage(out _);
            
            Assert.False(storage.SupportsDownloading);
            Assert.Throws<NotImplementedException>(() => { storage.GenerateDownloadUrl("some-location");});
        }
        
        
        private static DiskBlobStorage CreateBlobStorage(out string blobsPath)
        {
            var basePath = Path.GetTempPath();
            blobsPath = Path.Combine(basePath, "blobs");
            if (Directory.Exists(blobsPath))
            {
                Directory.Delete(blobsPath, true);
            }

            var context = FeatureEntryFacts.CreateTestSetupContext();
            return new DiskBlobStorage(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = basePath}));
        }
    }
}