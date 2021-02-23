using System.Collections.Generic;
using System.Linq;
using Moq;

namespace SharpCR.Registry.Tests.ControllerTests
{
    static class MockExtensions
    {
        
        public static Mock<IRepository<T>> InMockRepo<T>(this T item)
        {
            var mockRepoObj = new Mock<IRepository<T>>();
            mockRepoObj.Setup(r => r.All()).Returns(new []{ item }.AsQueryable());
            return mockRepoObj;
        }
        
        public static Mock<IRepository<T>> AsMockRepo<T>(this IEnumerable<T> items)
        {
            var mockRepoObj = new Mock<IRepository<T>>();
            mockRepoObj.Setup(r => r.All()).Returns(items.AsQueryable());
            return mockRepoObj;
        }
        
    }
}