using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class BlobControllerTests
    {
        [Fact]
        public void Get()
        {
            var repositoryName = "foo/abcd";
            var digest = "digest";
            var blobBytes = Encoding.Default.GetBytes("blob binary");

            var blobRecord = new BlobRecord {RepositoryName = repositoryName, DigestString = digest, ContentLength = blobBytes.Length};
            var dataStore = new RecordStoreStub().WithBlobs(blobRecord);

            var blobStorage = new BlobStorageStub();
            blobStorage.Save(repositoryName, digest, blobBytes);

            var controller = new BlobController(dataStore, blobStorage).SetupHttpContext();
            controller.HttpContext.Request.Method = "GET";

            var response = controller.Get(repositoryName, digest) as FileContentResult;

            Assert.NotNull(response);
            Assert.Equal(blobBytes.Length, response.FileContents.Length);
            Assert.Equal("application/octet-stream", response.ContentType);
            Assert.True(blobBytes.SequenceEqual(response.FileContents));
        }

        [Fact]
        public void Delete()
        {
            var repositoryName = "foo/abcd";
            var digest = "digest";
            var blobBytes = Encoding.Default.GetBytes("blob binary");

            var blobRecord = new BlobRecord {RepositoryName = repositoryName, DigestString = digest, ContentLength = blobBytes.Length};
            var dataStore = new RecordStoreStub().WithBlobs(blobRecord);

            var blobStorage = new BlobStorageStub();
            blobStorage.Save(repositoryName, digest, blobBytes);

            var controller = new BlobController(dataStore, blobStorage).SetupHttpContext();
            var response = controller.Delete(repositoryName, digest) as StatusCodeResult;

            Assert.NotNull(response);
            Assert.Equal(202, response.StatusCode);
            Assert.Null(dataStore.GetBlobByDigest(repositoryName, digest));
            Assert.Null(blobStorage.GetByDigest(repositoryName, digest));
        }
    }
}
