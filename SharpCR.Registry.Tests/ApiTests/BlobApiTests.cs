using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ApiTests
{
    public class BlobApiTests: IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly RecordStoreStub _stubRecordStore;
        
        private const string RepositoryName = "foo/bar";
        private const string DigestString= "sha256:cd12438ece47431a964d0e5712c1761e";

        public BlobApiTests(WebApplicationFactory<Startup> factory)
        {
            var blobStream = TestUtilities.GetManifestResourceStream("manifest.v2.json");
            var stubBlobStorage = new BlobStorageStub();
            _stubRecordStore = new RecordStoreStub().WithBlobs(new BlobRecord
                {
                    RepositoryName = RepositoryName,
                    DigestString = DigestString,
                    ContentLength = blobStream.Length,
                    MediaType = "application/vnd.docker.image.rootfs.diff.tar.gzip",
                    StorageLocation = stubBlobStorage.SaveAsync(RepositoryName, DigestString, blobStream).Result
                },
                new BlobRecord
                {
                    RepositoryName = RepositoryName,
                    DigestString = DigestString + "-deleted",
                    StorageLocation = "somewhere"
                });

            _client = factory.CreateClientWithServices((ctx, services) =>
            {
                services.AddSingleton<IBlobStorage>(stubBlobStorage);
                services.AddSingleton<IRecordStore>(_stubRecordStore);
            });
        }
        
        
        [Fact]
        public async Task GetBlob()
        {
            var response = await _client.GetAsync($"/v2/{RepositoryName}/blobs/{DigestString}");
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(893, response.Content.Headers.ContentLength);
            Assert.Equal("application/vnd.docker.image.rootfs.diff.tar.gzip", response.Content.Headers.ContentType.ToString());
            Assert.Equal(TestUtilities.GetManifestResource("manifest.v2.json"), content);
        }
        
        [Fact]
        public async Task HeadBlob()
        {
            var request = new HttpRequestMessage(HttpMethod.Head,$"/v2/{RepositoryName}/blobs/{DigestString}");
            var response = await _client.SendAsync(request);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(893, response.Content.Headers.ContentLength);
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }
        
        
        [Fact]
        public async Task DeleteBlob()
        {
            var response = await _client.DeleteAsync($"/v2/{RepositoryName}/blobs/{DigestString}-deleted");
            
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Null( await _stubRecordStore.GetBlobByDigestAsync(RepositoryName,$"{DigestString}-deleted"));
        }
        
        
        [Fact]
        public async Task PostUpload()
        {
            var response = await _client.PostAsync($"/v2/{RepositoryName}/blobs/uploads", new StringContent(string.Empty));
            
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Docker-Upload-UUID", out _));
        }
        
        
        [Fact]
        public async Task PatchUpload()
        {
            var response = await _client.PatchAsync($"/v2/{RepositoryName}/blobs/uploads/local_session", new StringContent(string.Empty));
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Docker-Upload-UUID", out _));
            Assert.False(response.Headers.TryGetValues("Range", out _));
        }
        
        [Fact]
        public async Task PutUpload()
        {
            var response = await _client.PutAsync($"/v2/{RepositoryName}/blobs/uploads/local_session", new StringContent(string.Empty));
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(response.Headers.TryGetValues("Docker-Upload-UUID", out _));
        }
        
    }
}