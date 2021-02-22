using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SharpCR.Registry.Tests.ApiTests
{
    public class BaseApiTests: IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;

        public BaseApiTests(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }
        
        [Fact]
        public async Task GetBase()
        {
            var response = await _client.GetAsync("/v2");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("registry/2.0", response.Headers.GetValues("Docker-Distribution-API-Version").Single());
            Assert.Equal("Authorization", response.Headers.Vary.Single());
            Assert.Equal(String.Empty, await response.Content.ReadAsStringAsync());
        }
    }
}