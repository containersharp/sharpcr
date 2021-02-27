using System;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class TagControllerTests
    {
        [Fact]
        public void ListTags()
        {
            var repoName = "foo/abcd";
            var dummyImage1 = new ImageRecord {Tag = "z1.0.0", RepositoryName = repoName};
            var dummyImage2 = new ImageRecord {Tag = "v1.0.0", RepositoryName = repoName};
            
            var controller = new TagController(new DataStoreStub(dummyImage1, dummyImage2));

            var tagResponse = controller.List(repoName, 1, null);
            
            Assert.NotNull(tagResponse);
            Assert.Equal(repoName, tagResponse.Value.name);
            Assert.Equal("v1.0.0", tagResponse.Value.tags[0]);
            Assert.Single(tagResponse.Value.tags);
        }
        

        
    }
}