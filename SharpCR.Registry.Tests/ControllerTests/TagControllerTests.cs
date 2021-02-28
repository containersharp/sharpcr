using SharpCR.Registry.Controllers;
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
            var dummyArtifact1 = new ArtifactRecord {Tag = "z1.0.0", RepositoryName = repoName};
            var dummyArtifact2 = new ArtifactRecord {Tag = "v1.0.0", RepositoryName = repoName};
            var dataStore = new RecordStoreStub().WithArtifacts(dummyArtifact1, dummyArtifact2);

            var controller = new TagController(dataStore);

            var tagResponse = controller.List(repoName, 1, null);

            Assert.NotNull(tagResponse);
            Assert.Equal(repoName, tagResponse.Value.name);
            Assert.Equal("v1.0.0", tagResponse.Value.tags[0]);
            Assert.Single(tagResponse.Value.tags);
        }
    }
}
