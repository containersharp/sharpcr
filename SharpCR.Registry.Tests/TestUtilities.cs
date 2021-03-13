using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SharpCR.Features;
using SharpCR.Registry.Features;
using SharpCR.Registry.Tests.ControllerTests;

namespace SharpCR.Registry.Tests
{
    public class TestUtilities
    {
        public static StartupContext CreateTestSetupContext(Dictionary<string, string> configValues = null)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues ?? new Dictionary<string, string>()).Build();
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

        public static string GetManifestResource(string name)
        {
            using var stream = typeof(ManifestControllerTests).Assembly.GetManifestResourceStream(
                $"SharpCR.Registry.Tests.ControllerTests.{name}");
            using var sr = new StreamReader(stream!);
            return sr.ReadToEnd();
        }
        
        public static Stream GetManifestResourceStream(string name)
        {
            return typeof(ManifestControllerTests).Assembly.GetManifestResourceStream(
                $"SharpCR.Registry.Tests.ControllerTests.{name}");
        }
    }
}