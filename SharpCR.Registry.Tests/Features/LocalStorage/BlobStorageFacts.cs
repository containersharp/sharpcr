using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            var location = await SaveByStorage(storage, bytes);

            var actualPath = Path.Combine(blobsPath, location);
            Assert.True(File.Exists(actualPath));
            Assert.True((await File.ReadAllBytesAsync(actualPath)).SequenceEqual(bytes));
        }

        [Fact]
        public async Task ShouldReadByLocation()
        {
            var storage = CreateBlobStorage(out _);

            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            var location = await SaveByStorage(storage, bytes);

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
        
        [Fact]
        public async Task ShouldLocateExistingBlobs()
        {
            var storage = CreateBlobStorage(out var blobPath);
            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));

            var digest = "sha256:ab123de";
            var location = await storage.SaveAsync(bytes.CreateTempFile(), "abc/foo",digest);
            var located = await storage.TryLocateExistingAsync(digest);
            
            Assert.Equal(location, located);
        }
        
        
        [Fact]
        public async Task ShouldIndexBlobs()
        {
            var storage = CreateBlobStorage(out var blobPath);
            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));
            
            var digest = "sha256:ab123de";
            var location = await storage.SaveAsync(bytes.CreateTempFile(),"abc/foo", digest);

            storage.Dispose();
            var indexContent = await File.ReadAllTextAsync(Path.Combine(blobPath, "index.txt"));
            Assert.Contains(location, indexContent);
            Assert.Contains(digest, indexContent);
        }

        [Fact]
        public async Task ShouldRemoveFromIndexAtNextLocatingAfterDelete()
        {
            var storage = CreateBlobStorage(out var blobPath);
            var bytes = Encoding.Default.GetBytes(Guid.NewGuid().ToString("N"));

            var digest = "sha256:ab123de";
            var location = await storage.SaveAsync(bytes.CreateTempFile(), "abc/foo", digest);
            await storage.DeleteAsync(location);
            await storage.TryLocateExistingAsync(digest);

            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            storage.Dispose();
            var indexContent = await File.ReadAllTextAsync(Path.Combine(blobPath, "index.txt"));
            Assert.DoesNotContain(location, indexContent);
            Assert.DoesNotContain(digest, indexContent);
        }


        static DiskBlobStorage CreateBlobStorage(out string blobsPath)
        {
            var basePath = Path.Combine(Path.GetTempPath(), "SharpCRTests", Guid.NewGuid().ToString("N"));
            blobsPath = Path.Combine(basePath, "blobs");
            var context = TestUtilities.CreateTestSetupContext();
            return new DiskBlobStorage(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = basePath}));
        }
        
        
        static async Task<string> SaveByStorage(DiskBlobStorage storage, byte[] bytes)
        {
            var file = new MemoryStream(bytes).CreateTempFile();
            return await storage.SaveAsync(file, "abc/foo", "sha256:ab123de");
        }


    }
}