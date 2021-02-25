using System.Collections.Generic;
using System.Linq;
using Moq;

namespace SharpCR.Registry.Tests.ControllerTests
{
    static class MockExtensions
    {
        
        public static Mock<IDataStore<T>> InMockStore<T>(this T item)
        {
            var mockRepoObj = new Mock<IDataStore<T>>();
            mockRepoObj.Setup(r => r.All()).Returns(new []{ item }.AsQueryable());
            return mockRepoObj;
        }
        
        public static Mock<IDataStore<T>> AsMockStore<T>(this IEnumerable<T> items)
        {
            var mockRepoObj = new Mock<IDataStore<T>>();
            mockRepoObj.Setup(r => r.All()).Returns(items.AsQueryable());
            return mockRepoObj;
        }
        
    }
}