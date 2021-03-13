using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SharpCR.Features.LocalStorage;
using Xunit;

namespace SharpCR.Registry.Tests.Features.LocalStorage
{
    public class BlobStorageFacts
    {
        [Fact]
        public async Task ShouldSaveBlob()
        {
            var storage = CreateBlobStorage(out var blobsPath);

            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            await using var ms = new MemoryStream(bytes);
            var location = await storage.SaveAsync("sha256@ab123de", ms, "abc/foo");

            var actualPath = Path.Combine(blobsPath, location);
            Assert.True(File.Exists(actualPath));
            Assert.True((await File.ReadAllBytesAsync(actualPath)).SequenceEqual(bytes));
        }
        
        [Fact]
        public async Task ShouldReadByLocation()
        {
            var storage = CreateBlobStorage(out _);

            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            await using var ms = new MemoryStream(bytes);
            var location = await storage.SaveAsync("sha256@ab123de", ms, "abc/foo");

            await using var readResult = await storage.ReadAsync(location);
            await using var readMs = new MemoryStream();
            await readResult.CopyToAsync(readMs);
            var readBytes = readMs.ToArray();
            
            Assert.True(readBytes.SequenceEqual(bytes));
        }


        [Fact]
        public void ShouldNotSupportDownloads()
        {
            var storage = CreateBlobStorage(out _);
            
            Assert.False(storage.SupportsDownloading);
            Assert.Throws<NotImplementedException>(() => { storage.GenerateDownloadUrlAsync("some-location");});
        }
        
        
        private static DiskBlobStorage CreateBlobStorage(out string blobsPath)
        {
            var basePath = Path.GetTempPath();
            blobsPath = Path.Combine(basePath, "blobs");
            var context = TestUtilities.CreateTestSetupContext();
            return new DiskBlobStorage(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = basePath}));
        }
    }
}