using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SharpCR.Features.LocalStorage;
using SharpCR.Registry.Features;
using Xunit;

namespace SharpCR.Registry.Tests.Features.LocalStorage
{
    public class FeatureEntryFacts
    {
        private readonly Type _featureType = typeof(LocalStorageFeature);

        [Fact]
        public void ShouldCreateFeatureInstance()
        {
            var featureObject = CreateFeatureInstance(_featureType);
            
            Assert.NotNull(featureObject);
        }

        static IFeature CreateFeatureInstance(Type featureType)
        {
            return Activator.CreateInstance(featureType) as IFeature;
        }

        [Fact]
        public void ShouldConfigureServices()
        {
            var services = new ServiceCollection();

            var featureObject = CreateFeatureInstance(_featureType);
            featureObject.ConfigureServices(services, CreateTestSetupContext());
            
            Assert.NotEmpty(services);
            Assert.NotNull(featureObject);
        }
        
        
        [Fact]
        public void ShouldConfigureAppPipeline()
        {
            var services = new ServiceCollection().BuildServiceProvider();

            var featureObject = CreateFeatureInstance(_featureType);
            featureObject.ConfigureWebAppPipeline(new ApplicationBuilder(services), services, CreateTestSetupContext());
            
            Assert.NotNull(featureObject);
        }

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