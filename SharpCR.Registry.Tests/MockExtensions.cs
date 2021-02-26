using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Tests
{
    static class MockExtensions
    {
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