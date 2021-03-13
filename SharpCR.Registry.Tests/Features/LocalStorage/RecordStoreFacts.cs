using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SharpCR.Features.LocalStorage;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.Features.LocalStorage
{
    public class RecordStoreFacts
    {
        [Fact]
        public async Task ShouldStore()
        {
            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var recordsFile = Path.Combine(basePath, "records.json");
            if (File.Exists(recordsFile))
            {
                File.Delete(recordsFile);
            }
            var store = CreateRecordStore(basePath);

            var artifact = new ArtifactRecord()
            {
                Tag = null,
                DigestString = "sha256:foobar",
                RepositoryName = "library/abcd"
            };
            await store.CreateArtifactAsync(artifact);
            
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.True(File.Exists(recordsFile));
            Assert.True(new FileInfo(recordsFile).Length > 0);
        }
        
        [Fact]
        public async Task ShouldGet()
        {
            var store = CreateRecordStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            var digestString = "sha256:" + Guid.NewGuid().ToString("N");
            var repositoryName = "library/abcd";

            var artifact = new ArtifactRecord()
            {
                Tag = null,
                DigestString = digestString,
                RepositoryName = repositoryName
            };
            await store.CreateArtifactAsync(artifact);

            var storedItem = await store.GetArtifactByDigestAsync(repositoryName, digestString);
            Assert.NotNull(storedItem);
        }

        private static DiskRecordStore CreateRecordStore(string path)
        {
            var context = TestUtilities.CreateTestSetupContext();
            return new DiskRecordStore(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = path}));
        }
    }
}