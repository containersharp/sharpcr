using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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

        public static HttpClient CreateClientWithServices<TStartup>(this WebApplicationFactory<TStartup> factory, Action<WebHostBuilderContext, IServiceCollection> services) where TStartup: class
        {
            return factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services);
                })
                .CreateClient(new WebApplicationFactoryClientOptions() {AllowAutoRedirect = false});
        }
        
        public static HttpClient CreateClient<TStartup>(this WebApplicationFactory<TStartup> factory, Action<WebHostBuilderContext, IServiceCollection> services) where TStartup: class
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions() {AllowAutoRedirect = false});
        }
        
    }
}