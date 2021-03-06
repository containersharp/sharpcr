using System;
using System.Collections.Generic;
using System.Linq;
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
            featureObject.ConfigureServices(services, TestUtilities.CreateTestSetupContext());
            
            Assert.NotNull(featureObject);
            Assert.NotNull(services.FirstOrDefault(s => s.ImplementationType == typeof(DiskRecordStore)));
            Assert.NotNull(services.FirstOrDefault(s => s.ImplementationType == typeof(DiskBlobStorage)));
        }
        
        
        [Fact]
        public void ShouldConfigureAppPipeline()
        {
            var services = new ServiceCollection().BuildServiceProvider();

            var featureObject = CreateFeatureInstance(_featureType);
            featureObject.ConfigureWebAppPipeline(new ApplicationBuilder(services), services, TestUtilities.CreateTestSetupContext());
            
            Assert.NotNull(featureObject);
        }
    }
}