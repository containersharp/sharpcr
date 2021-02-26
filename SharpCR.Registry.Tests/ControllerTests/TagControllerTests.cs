using System;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.Models;
using Xunit;

namespace SharpCR.Registry.Tests.ControllerTests
{
    public class TagControllerTests
    {
        [Fact]
        public void ListTags()
        {
            var dummyImageRepo2 = new ImageRepository {Name = "foo/abcd"};
            var dummyImage1 = new Image {Tag = "z1.0.0", RepositoryName = dummyImageRepo2.Name};
            var dummyImage2 = new Image {Tag = "v1.0.0", RepositoryName = dummyImageRepo2.Name};
            
            var controller = new TagController(new DataStoreStub(dummyImage1, dummyImage2));

            var tagResponse = controller.List(dummyImageRepo2.Name, 1, null);
            
            Assert.NotNull(tagResponse);
            Assert.Equal("foo/abcd", tagResponse.Value.name);
            Assert.Equal("v1.0.0", tagResponse.Value.tags[0]);
            Assert.Single(tagResponse.Value.tags);
        }
        

        
    }
}