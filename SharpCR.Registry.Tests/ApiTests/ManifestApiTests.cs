using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharpCR.Features;
using SharpCR.Features.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ApiTests
{
    public class ManifestApiTests: IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;
        private readonly RecordStoreStub _stubRecordStore;
        
        private const string RepositoryName = "foo/bar";
        private const string DigestString= "sha256:cd12438ece47431a964d0e5712c1761e";
        private const string Tag = "latest";

        public ManifestApiTests(WebApplicationFactory<Startup> factory)
        {
            var manifestContent = TestUtilities.GetManifestResource("manifest.v2.json");
            _stubRecordStore = new RecordStoreStub().WithArtifacts(new ArtifactRecord
                {
                    RepositoryName = RepositoryName,
                    DigestString = DigestString,
                    Tag = Tag,
                    ManifestMediaType = WellKnownMediaTypes.DockerImageManifestV2,
                    ManifestBytes = Encoding.Default.GetBytes(manifestContent)
                },
                new ArtifactRecord
                {
                    RepositoryName = RepositoryName,
                    Tag = "deleted"
                });

            _client = factory.CreateClientWithServices((ctx, services) =>
            {
                services.AddSingleton<IRecordStore>(_stubRecordStore);
            });
        }
        
        
        [Fact]
        public async Task GetManifest()
        {
            var response = await _client.GetAsync($"/v2/{RepositoryName}/manifests/{DigestString}");
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(893, response.Content.Headers.ContentLength);
            Assert.Equal(WellKnownMediaTypes.DockerImageManifestV2, response.Content.Headers.ContentType.ToString());
            Assert.Equal(TestUtilities.GetManifestResource("manifest.v2.json"), content);
        }
        
        [Fact]
        public async Task HeadManifest()
        {
            var request = new HttpRequestMessage(HttpMethod.Head,$"/v2/{RepositoryName}/manifests/{DigestString}");
            var response = await _client.SendAsync(request);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(893, response.Content.Headers.ContentLength);
            Assert.Equal(WellKnownMediaTypes.DockerImageManifestV2, response.Content.Headers.ContentType.ToString());
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }
        
        
        [Fact]
        public async Task DeleteManifest()
        {
            var response = await _client.DeleteAsync($"/v2/{RepositoryName}/manifests/deleted");
            
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Null( await _stubRecordStore.GetArtifactByTagAsync(RepositoryName,$"deleted"));
        }
        
        
        [Fact]
        public async Task SaveManifest()
        {
            var response = await _client.PutAsync($"/v2/{RepositoryName}/manifests/new", new StringContent(string.Empty));
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        
    }
}