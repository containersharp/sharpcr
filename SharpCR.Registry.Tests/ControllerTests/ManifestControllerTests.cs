using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class ManifestControllerTests
    {
        
        [Fact]
        public void GetManifest()
        {
            var dummyImageRepo2 = new RepositoryRecord {Name = "foo/abcd"};
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest());
            var manifestType = "application/vnd.docker.distribution.manifest.v1+json";
            var dummyImage1 = new ImageRecord {Tag = "z1.0.0", RepositoryName = dummyImageRepo2.Name, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyImage2 = new ImageRecord {Tag = "v1.0.0", RepositoryName = dummyImageRepo2.Name, ManifestBytes = manifestBytes, ManifestMediaType = manifestType };
            
            var controller = new ManifestController(new DataStoreStub(dummyImage1, dummyImage2 )).SetupHttpContext();
            controller.HttpContext.Request.Method = "GET";

            var manifestResponse = controller.Get(dummyImageRepo2.Name, "v1.0.0");
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
            
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest());
            var manifestType = "application/vnd.docker.distribution.manifest.v1+json";
            var dummyImage1 = new ImageRecord {Tag = "z1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyImage2 = new ImageRecord {Tag = "v1.0.0", RepositoryName = repositoryName, ManifestBytes = manifestBytes, ManifestMediaType = manifestType };
            var dataStore = new DataStoreStub(dummyImage1, dummyImage2 );
            
            var controller = new ManifestController(dataStore).SetupHttpContext();
            var imageTag = "v1.0.0";
            var deleteResponse = controller.Delete(repositoryName, imageTag);
            var statusCodeResult = deleteResponse as StatusCodeResult;
            
            Assert.NotNull(statusCodeResult);
            Assert.Equal(202, statusCodeResult.StatusCode);
            Assert.Null(dataStore.GetImagesByTag(repositoryName, imageTag));
            
            // todo: assert orphan blobs are deleted while referenced blobs are kept. 
        }
        
        
        
        

        private static string getImageManifest()
        {
            using var stream = typeof(ManifestControllerTests).Assembly.GetManifestResourceStream(
                    "SharpCR.Registry.Tests.ControllerTests.dummymanifest.json");
            using var sr = new StreamReader(stream!);
            return sr.ReadToEnd();

        }
    }
}