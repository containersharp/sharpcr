using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers;
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
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dummyImage1 = new ImageRecord {Tag = "z1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyImage2 = new ImageRecord {Tag = "v1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType };
            
            var controller = new ManifestController(new DataStoreStub(dummyImage1, dummyImage2 )).SetupHttpContext();
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
            const string imageTag = "v1.0.0";
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dummyImage1 = new ImageRecord {Tag = "z1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyImage2 = new ImageRecord {Tag = "v1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType };
            var dataStore = new DataStoreStub(dummyImage1, dummyImage2 );
            
            var controller = new ManifestController(dataStore).SetupHttpContext();
            var deleteResponse = controller.Delete(repositoryName, imageTag);
            var statusCodeResult = deleteResponse as StatusCodeResult;
            
            Assert.NotNull(statusCodeResult);
            Assert.Equal(202, statusCodeResult.StatusCode);
            Assert.Null(dataStore.GetImagesByTag(repositoryName, imageTag));
        }
        
        [Fact]
        public void PutManifest_V2_Schema()
        {
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest("manifest.v2.json"));
            var manifestType = "application/vnd.docker.distribution.manifest.v2+json";
            var dataStore = new DataStoreStub();
            
            var controller = new ManifestController(dataStore).SetupHttpContext();
            var request = controller.Request;
            request.Headers.Add("Content-Type", manifestType);
            request.Body = new MemoryStream(manifestBytes);
            
            const string repositoryName = "foo/abcd";
            const string imageTag = "v1.0.0";
            var putResponse = controller.Save(repositoryName, imageTag);
            var statusCodeResult = putResponse as StatusCodeResult;
            var storedImage = dataStore.GetImagesByTag(repositoryName, imageTag);
            
            Assert.NotNull(statusCodeResult);
            Assert.Equal(201, statusCodeResult.StatusCode);
            Assert.Equal(controller.Response.Headers["Docker-Content-Digest"].ToString(), storedImage.DigestString); 
            Assert.True(!string.IsNullOrWhiteSpace(controller.Response.Headers["Location"].ToString())); 
            Assert.NotNull(storedImage); 
            Assert.Equal(manifestType, storedImage.ManifestMediaType); 
        }
        
        
        
            
        // todo: assert orphan blobs are deleted when deleting manifest while referenced blobs are kept. 
        // todo: test more put cases 

        private static string getImageManifest(string name)
        {
            using var stream = typeof(ManifestControllerTests).Assembly.GetManifestResourceStream(
                    $"SharpCR.Registry.Tests.ControllerTests.{name}");
            using var sr = new StreamReader(stream!);
            return sr.ReadToEnd();

        }
    }
}