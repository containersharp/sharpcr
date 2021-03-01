using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Models.Manifests;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class ManifestControllerTests
    {
        [Fact]
        public void GetManifest()
        {
            var repositoryName = "foo/abcd";
            var manifestBytes = Encoding.Default.GetBytes(getManifestResource("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dummyArtifact1 = new ArtifactRecord {Tag = "z1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyArtifact2 = new ArtifactRecord {Tag = "v1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dataStore = new RecordStoreStub().WithArtifacts(dummyArtifact1, dummyArtifact2);

            var controller = new ManifestController(dataStore).SetupHttpContext();
            controller.HttpContext.Request.Method = "GET";

            var manifestResponse = controller.Get(repositoryName, "v1.0.0");
            var fileResult = manifestResponse as FileContentResult;

            Assert.NotNull(fileResult);
            Assert.Equal(manifestBytes.Length, fileResult.FileContents.Length);
            Assert.Equal(manifestType, fileResult.ContentType);
            Assert.True(manifestBytes.SequenceEqual(fileResult.FileContents));
        }

        [Fact]
        public void DeleteManifest()
        {
            const string repositoryName = "foo/abcd";
            const string tag = "v1.0.0";
            var manifestBytes = Encoding.Default.GetBytes(getManifestResource("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dummyArtifact1 = new ArtifactRecord {Tag = "z1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyArtifact2 = new ArtifactRecord {Tag = "v1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dataStore = new RecordStoreStub().WithArtifacts(dummyArtifact1, dummyArtifact2);

            var controller = new ManifestController(dataStore).SetupHttpContext();
            var deleteResponse = controller.Delete(repositoryName, tag);
            var statusCodeResult = deleteResponse as StatusCodeResult;

            Assert.NotNull(statusCodeResult);
            Assert.Equal(202, statusCodeResult.StatusCode);
            Assert.Null(dataStore.GetArtifactByTag(repositoryName, tag));
        }

        [Fact]
        public void PutManifest_V2_Schema()
        {
            const string repositoryName = "foo/abcd";
            const string tag = "v1.0.0";
            var manifestBytes = Encoding.Default.GetBytes(getManifestResource("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dataStore = new RecordStoreStub().WithBlobs(BuildBlobRecords(repositoryName, manifestBytes));

            var controller = new ManifestController(dataStore).SetupHttpContext();
            controller.Request.Headers.Add("Content-Type", manifestType);
            controller.Request.Body = new MemoryStream(manifestBytes);

            var putResponse = controller.Save(repositoryName, tag);
            var statusCodeResult = putResponse as StatusCodeResult;
            var storedArtifact = dataStore.GetArtifactByTag(repositoryName, tag);

            Assert.NotNull(statusCodeResult);
            Assert.Equal(201, statusCodeResult.StatusCode);
            Assert.Equal(controller.Response.Headers["Docker-Content-Digest"].ToString(), storedArtifact.DigestString);
            Assert.True(!string.IsNullOrWhiteSpace(controller.Response.Headers["Location"].ToString()));
            Assert.NotNull(storedArtifact);
            Assert.Equal(manifestType, storedArtifact.ManifestMediaType);
        }

        // todo: assert orphan blobs are deleted when deleting manifest while referenced blobs are kept.
        // todo: test more put cases

        private static string getManifestResource(string name)
        {
            using var stream = typeof(ManifestControllerTests).Assembly.GetManifestResourceStream(
                $"SharpCR.Registry.Tests.ControllerTests.{name}");
            using var sr = new StreamReader(stream!);
            return sr.ReadToEnd();
        }

        private BlobRecord[] BuildBlobRecords(string repositoryName, byte[] manifestBytes)
        {
            var manifest = new ManifestV2.Parser().Parse(manifestBytes);
            var references = manifest.GetReferencedDescriptors();

            return references.Select(
                e => new BlobRecord
                {
                    RepositoryName = repositoryName,
                    DigestString = e.Digest,
                    ContentLength = e.Size ?? 0,
                    Location = ""
                }).ToArray();
        }
    }
}
