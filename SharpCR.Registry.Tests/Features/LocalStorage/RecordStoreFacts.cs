using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Options;
using SharpCR.Features.LocalStorage;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.Features.LocalStorage
{
    public class RecordStoreFacts
    {
        [Fact]
        public void ShouldStore()
        {
            var basePath = Path.GetTempPath();
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
            store.CreateArtifact(artifact);
            
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.True(File.Exists(recordsFile));
            Assert.True(new FileInfo(recordsFile).Length > 0);
        }
        
        [Fact]
        public void ShouldGet()
        {
            var store = CreateRecordStore(Path.GetTempPath());
            var digestString = "sha256:" + Guid.NewGuid().ToString("N");
            var repositoryName = "library/abcd";

            var artifact = new ArtifactRecord()
            {
                Tag = null,
                DigestString = digestString,
                RepositoryName = repositoryName
            };
            store.CreateArtifact(artifact);

            var storedItem = store.GetArtifactByDigest(repositoryName, digestString);
            Assert.NotNull(storedItem);
        }

        private static DiskRecordStore CreateRecordStore(string path)
        {
            var context = FeatureEntryFacts.CreateTestSetupContext();
            return new DiskRecordStore(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = path}));
        }
    }
}