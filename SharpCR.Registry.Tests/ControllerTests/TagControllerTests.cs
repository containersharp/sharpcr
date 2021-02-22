using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using SharpCR.Registry.Controllers;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class TagControllerTests
    {
        [Fact]
        public void ListTags()
        {
            var controller = new TagController();
            var repoName = "test";

            var tagResponse = controller.List(repoName, null, null);
            
            Assert.NotNull(tagResponse);
            Assert.Equal(repoName, tagResponse.name);
        }
        

    }
}