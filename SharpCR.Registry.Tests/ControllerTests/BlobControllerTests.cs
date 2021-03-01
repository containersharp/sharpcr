using System.IO;
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

            var blobStream = new MemoryStream(Encoding.Default.GetBytes("blob binary"));
            var blobStorage = new BlobStorageStub();
            var blobUrl = blobStorage.Save(repositoryName, digest, blobStream);

            var blobRecord = new BlobRecord {RepositoryName = repositoryName, DigestString = digest, ContentLength = blobStream.Length, Url = blobUrl};
            var dataStore = new RecordStoreStub().WithBlobs(blobRecord);

            var controller = new BlobController(dataStore, blobStorage).SetupHttpContext();
            controller.HttpContext.Request.Method = "GET";

            var response = controller.Get(repositoryName, digest) as FileStreamResult;
            var responseStream = new MemoryStream();

            response?.FileStream.CopyTo(responseStream);

            Assert.NotNull(response);
            Assert.Equal(blobStream.Length, response.FileStream.Length);
            Assert.Equal("application/octet-stream", response.ContentType);
            Assert.True(blobStream.ToArray().SequenceEqual(responseStream.ToArray()));
        }

        [Fact]
        public void Delete()
        {
            var repositoryName = "foo/abcd";
            var digest = "digest";

            var blobStream = new MemoryStream(Encoding.Default.GetBytes("blob binary"));
            var blobStorage = new BlobStorageStub();
            var blobUrl = blobStorage.Save(repositoryName, digest, blobStream);

            var blobRecord = new BlobRecord {RepositoryName = repositoryName, DigestString = digest, ContentLength = blobStream.Length, Url = blobUrl};
            var dataStore = new RecordStoreStub().WithBlobs(blobRecord);

            var controller = new BlobController(dataStore, blobStorage).SetupHttpContext();
            var response = controller.Delete(repositoryName, digest) as StatusCodeResult;

            Assert.NotNull(response);
            Assert.Equal(202, response.StatusCode);
            Assert.Null(dataStore.GetBlobByDigest(repositoryName, digest));
            Assert.Null(blobStorage.GetByDigest(blobUrl));
        }
    }
}
