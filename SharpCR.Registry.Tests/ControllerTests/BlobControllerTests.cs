using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharpCR.Features;
using SharpCR.Features.Records;
using SharpCR.Registry.Controllers;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class BlobControllerTests
    {
        [Fact]
        public async Task Get()
        {
            var repositoryName = "foo/abcd";
            var digest = "digest";
            var blobStream = new MemoryStream(Encoding.Default.GetBytes("blob binary"));
            var blobStorage = new BlobStorageStub();
            var dataStore = new RecordStoreStub().WithBlobs(
                new BlobRecord
                {
                    RepositoryName = repositoryName,
                    DigestString = digest, ContentLength = blobStream.Length,
                    StorageLocation = await blobStorage.SaveAsync( blobStream.CreateTempFile(),  repositoryName, digest)
                });

            var controller = CreateController(dataStore, blobStorage);
            controller.HttpContext.Request.Method = "GET";
            var response = await controller.Get(repositoryName, digest) as FileStreamResult;

            Assert.NotNull(response);
            await using var responseStream = new MemoryStream();
            await response.FileStream.CopyToAsync(responseStream);
            Assert.Equal(blobStream.Length, response.FileStream.Length);
            Assert.Equal("application/octet-stream", response.ContentType);
            Assert.True(blobStream.ToArray().SequenceEqual(responseStream.ToArray()));
        }

        [Fact]
        public async Task Delete()
        {
            var repositoryName = "foo/abcd";
            var digest = "digest";

            var blobStream = new MemoryStream(Encoding.Default.GetBytes("blob binary"));
            var blobStorage = new BlobStorageStub();
            var blobLocation = await blobStorage.SaveAsync(blobStream.CreateTempFile(),  repositoryName, digest);

            var blobRecord = new BlobRecord {RepositoryName = repositoryName, DigestString = digest, ContentLength = blobStream.Length, StorageLocation = blobLocation};
            var dataStore = new RecordStoreStub().WithBlobs(blobRecord);

            var controller = CreateController(dataStore, blobStorage);
            var response = await controller.Delete(repositoryName, digest) as ObjectResult;

            Assert.NotNull(response);
            Assert.Equal(202, response.StatusCode);
            Assert.Null(await dataStore.GetBlobByDigestAsync(repositoryName, digest));
            Assert.Null(await blobStorage.ReadAsync(blobLocation));
        }
        // todo: no blobs should be deleted if they are referenced by any manifest

        [Fact]
        public async Task CreateUpload()
        {
            var controller = CreateController(new RecordStoreStub(), new BlobStorageStub());
            controller.Request.Method = "POST";

            var response = await controller.CreateUpload("abcd/foo-bar", null, null, null) as AcceptedResult;

            Assert.NotNull(response);
            Assert.Equal(202, response.StatusCode);
            Assert.NotEmpty(response.Location);
            Assert.NotEmpty(controller.Response.Headers["Docker-Upload-UUID"].ToString());
        }

        [Fact]
        public async Task MonolithicUpload()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            var contentDigest = SendStreamByRequest(controller);

            var response = await controller.CreateUpload("abcd/foo-bar", contentDigest.ToString(), null, null) as CreatedResult;

            Assert.NotNull(response);
            Assert.Equal(201, response.StatusCode);
            Assert.NotEmpty(response.Location);
            Assert.NotEmpty(controller.Response.Headers["Docker-Content-Digest"].ToString());
            Assert.Empty(controller.Response.Headers["Docker-Upload-UUID"].ToString());

            Assert.NotNull(await recordStore.GetBlobByDigestAsync("abcd/foo-bar", contentDigest.ToString()));
            Assert.Equal(contentDigest, Digest.Compute(blobStorage.GetStoredBlobs().Single()));
        }

        [Fact]
        public async Task MountUpload()
        {
            var repo1 = "repo1";
            var repo2 = "repo2";
            var digest = "digest";

            var blobRecord1 = new BlobRecord
            {
                RepositoryName = "library/repo1",
                DigestString = digest,
                ContentLength = 100,
                StorageLocation = "location"
            };
            var recordStore = new RecordStoreStub().WithBlobs(blobRecord1);

            var controller = CreateController(recordStore, null);
            var response = await controller.CreateUpload(repo2, "", digest, repo1) as CreatedResult;

            Assert.NotNull(response);
            Assert.Equal((int) HttpStatusCode.Created, response.StatusCode);
            Assert.NotEmpty(response.Location);

            var blobRecord2 = await  recordStore.GetBlobByDigestAsync(repo2, digest);
            Assert.NotNull(blobRecord2);
        }

        [Fact]
        public async Task ContinueUpload()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            SendStreamByRequest(controller, "PATCH");
            var sessionId = $"{new Settings().BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";

            var response = await controller.ContinueUpload("abcd/foo-bar", sessionId, null) as AcceptedResult;

            Assert.NotNull(response);
            Assert.NotEmpty(response.Location);
            Assert.NotEmpty(controller.Response.Headers["Range"].ToString());
            Assert.Equal(sessionId, controller.Response.Headers["Docker-Upload-UUID"].ToString());
            Assert.Empty(blobStorage.GetStoredBlobs());
        }

        [Fact]
        public async Task ContinueUploadWithChunkedEncoding()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            var contentDigest = SendStreamByRequest(controller, "PATCH");
            var sendLength = controller.Request.ContentLength!.Value;
            controller.Request.Headers["Transfer-Encoding"] = "chunked";
            controller.Request.ContentLength = null;
            var sessionId = $"{new Settings().BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";

            var response = await controller.ContinueUpload("abcd/foo-bar", sessionId, null) as AcceptedResult;

            Assert.NotNull(response);
            Assert.Equal(202, response.StatusCode);
            Assert.NotEmpty(response.Location);
            Assert.NotEmpty(controller.Response.Headers["Docker-Upload-UUID"].ToString());
            Assert.Empty(controller.Response.Headers["Docker-Content-Digest"].ToString());
            Assert.Equal($"0-{sendLength - 1}", controller.Response.Headers["Range"].ToString());

            Assert.Null(await recordStore.GetBlobByDigestAsync("abcd/foo-bar", contentDigest.ToString()));
            Assert.Empty(blobStorage.GetStoredBlobs());
        }

        [Fact]
        public async Task FinishUploadWithChunk()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            var contentDigest = SendStreamByRequest(controller, "PUT");
            var sessionId = $"{new Settings().BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";

            var response = await controller.ContinueUpload("abcd/foo-bar", sessionId, contentDigest.ToString()) as CreatedResult;

            Assert.NotNull(response);
            Assert.Equal(201, response.StatusCode);
            Assert.NotEmpty(response.Location);
            Assert.Empty(controller.Response.Headers["Docker-Upload-UUID"].ToString());
            Assert.Equal(contentDigest.ToString(), controller.Response.Headers["Docker-Content-Digest"].ToString());

            Assert.NotNull(await recordStore.GetBlobByDigestAsync("abcd/foo-bar", contentDigest.ToString()));
            Assert.Equal(contentDigest, Digest.Compute(blobStorage.GetStoredBlobs().Single()));
        }

        [Fact]
        public async Task FinishUploadWithChunkedEncoding()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            var contentDigest = SendStreamByRequest(controller, "PUT");
            controller.Request.Headers["Transfer-Encoding"] = "chunked";
            controller.Request.ContentLength = null;
            var sessionId = $"{new Settings().BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";

            var response = await controller.ContinueUpload("abcd/foo-bar", sessionId, contentDigest.ToString()) as CreatedResult;

            Assert.NotNull(response);
            Assert.Equal(201, response.StatusCode);
            Assert.NotEmpty(response.Location);
            Assert.Empty(controller.Response.Headers["Docker-Upload-UUID"].ToString());
            Assert.NotEmpty(controller.Response.Headers["Docker-Content-Digest"].ToString());
            Assert.Empty(controller.Response.Headers["Range"].ToString());

            Assert.NotNull(await recordStore.GetBlobByDigestAsync("abcd/foo-bar", contentDigest.ToString()));
            Assert.Equal(contentDigest, Digest.Compute(blobStorage.GetStoredBlobs().Single()));
        }

        [Fact]
        public async Task FinishUploadWithoutChunk()
        {
            var recordStore = new RecordStoreStub();
            var blobStorage = new BlobStorageStub();
            var controller = CreateController(recordStore, blobStorage);
            var contentDigest = SendStreamByRequest(controller, "PATCH");
            var sessionId = $"{new Settings().BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";
            await controller.ContinueUpload("abcd/foo-bar", sessionId, null);
            controller.Request.Method = "PUT";
            controller.Request.Body = null;
            controller.Request.ContentLength = 0;
            controller.Response.Headers.Clear();

            var createdResult = await controller.ContinueUpload("abcd/foo-bar", sessionId, null) as CreatedResult;


            Assert.NotNull(createdResult);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.NotEmpty(createdResult.Location);
            Assert.Empty(controller.Response.Headers["Docker-Upload-UUID"].ToString());
            Assert.Equal(contentDigest.ToString(), controller.Response.Headers["Docker-Content-Digest"].ToString());

            Assert.NotNull(await recordStore.GetBlobByDigestAsync("abcd/foo-bar", contentDigest.ToString()));
            Assert.Equal(contentDigest, Digest.Compute(blobStorage.GetStoredBlobs().Single()));
        }

        private static Digest SendStreamByRequest(BlobController controller, string httpMethod = "POST")
        {
            controller.Request.Method = httpMethod;
            var requestBody = TestUtilities.GetManifestResourceStream("manifest.v2.json");
            controller.Request.Body = requestBody;
            controller.Request.ContentLength = requestBody.Length;

            var digest = Digest.Compute(requestBody);
            requestBody.Seek(0, SeekOrigin.Begin);
            return digest;
        }

        private BlobController CreateController(IRecordStore dataStore, IBlobStorage blobStorage)
        {
            var settings = Options.Create(new Settings { });
            var env = TestUtilities.CreateTestSetupContext().HostEnvironment;

            return new BlobController(dataStore, blobStorage, settings, env, NullLogger<BlobController>.Instance).SetupHttpContext();
        }
    }
}
