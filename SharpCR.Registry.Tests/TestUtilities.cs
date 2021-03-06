using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SharpCR.Registry.Features;

namespace SharpCR.Registry.Tests
{
    public class TestUtilities
    {
        public static StartupContext CreateTestSetupContext()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fileProvider = new PhysicalFileProvider(basePath);
            var hostEnv = new TestWebEnvironment()
            {
                EnvironmentName = "Development",
                ApplicationName = "SharpCR",
                ContentRootPath = basePath,
                WebRootPath = basePath,
                ContentRootFileProvider = fileProvider,
                WebRootFileProvider = fileProvider
            };
            
            return new StartupContext()
            {
                HostEnvironment = hostEnv,
                Configuration = configuration
            };
        }
        
        class TestWebEnvironment: IWebHostEnvironment
        {
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string WebRootPath { get; set; }
        }
    }
}