using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Models;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class ManifestControllerTests
    {
        
        [Fact]
        public void GetManifest()
        {
            var dummyImageRepo1 = new ImageRepository {Name = "bar/abcd", Id = Guid.NewGuid()};
            var dummyImageRepo2 = new ImageRepository {Name = "foo/abcd", Id = Guid.NewGuid()};
            var manifestBytes = Encoding.Default.GetBytes(getImageManifest());
            var manifestType = "application/vnd.docker.distribution.manifest.v1+json";
            var dummyImage1 = new Image {Tag = "z1.0.0", RepositoryName = dummyImageRepo2.Name, ManifestBytes = manifestBytes, ManifestMediaType = manifestType};
            var dummyImage2 = new Image {Tag = "v1.0.0", RepositoryName = dummyImageRepo2.Name, ManifestBytes = manifestBytes, ManifestMediaType = manifestType };
            
            var controller = new ManifestController(
                (new []{dummyImageRepo1, dummyImageRepo2 }).AsMockStore().Object,
                (new []{dummyImage1, dummyImage2 }).AsMockStore().Object).SetupHttpContext();
            controller.HttpContext.Request.Method = "GET";

            var manifestResponse = controller.Get(dummyImageRepo2.Name, "v1.0.0");
            var fileResult = manifestResponse as FileContentResult;
            
            Assert.NotNull(fileResult);
            Assert.Equal(manifestBytes.Length, fileResult.FileContents.Length);
            Assert.Equal(manifestType, fileResult.ContentType);
            Assert.True(manifestBytes.SequenceEqual(fileResult.FileContents));
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