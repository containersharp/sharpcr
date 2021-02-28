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
            var tempPath = Path.GetTempPath();
            var storePath = Path.Combine(tempPath, "registry.json");
            if (File.Exists(storePath))
            {
                File.Delete(storePath);
            }
            var store = CreateRecordStore(tempPath);

            var artifact = new ArtifactRecord()
            {
                Tag = null,
                DigestString = "sha256:foobar",
                RepositoryName = "library/abcd"
            };
            store.CreateArtifact(artifact);
            
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.True(File.Exists(storePath));
            Assert.True(new FileInfo(storePath).Length > 0);
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

        private static RecordStore CreateRecordStore(string path)
        {
            var context = FeatureEntryFacts.CreateTestSetupContext();
            return new RecordStore(context.HostEnvironment, Options.Create(new LocalStorageConfiguration{ BasePath = path}));
        }
    }
}