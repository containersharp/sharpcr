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
            var dummyImageRepo1 = new ImageRepository {Name = "bar/abcd", Id = Guid.NewGuid()};
            var dummyImageRepo2 = new ImageRepository {Name = "foo/abcd", Id = Guid.NewGuid()};
            var dummyImageTag1 = new Tag {Name = "z1.0.0", RepositoryId = dummyImageRepo2.Id};
            var dummyImageTag2 = new Tag {Name = "v1.0.0", RepositoryId = dummyImageRepo2.Id};
            
            var controller = new TagController(
                (new []{dummyImageTag1, dummyImageTag2 }).AsMockStore().Object, 
                (new []{dummyImageRepo1, dummyImageRepo2 }).AsMockStore().Object);

            var tagResponse = controller.List(dummyImageRepo2.Name, 1, null);
            
            Assert.NotNull(tagResponse);
            Assert.Equal("foo/abcd", tagResponse.Value.name);
            Assert.Equal("v1.0.0", tagResponse.Value.tags[0]);
            Assert.Single(tagResponse.Value.tags);
        }
        

        
    }
}