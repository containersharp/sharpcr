using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public static T SetupHttpContext<T>(this T controller, HttpContext httpContext = null) where T: ControllerBase
        {
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext ?? new DefaultHttpContext()
            };
            return controller;
        }
        
    }
}