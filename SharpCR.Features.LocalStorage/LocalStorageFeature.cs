using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharpCR.Registry;
using SharpCR.Registry.Features;

namespace SharpCR.Features.LocalStorage
{
    public class LocalStorageFeature : IFeature
    {
        public void ConfigureServices(IServiceCollection services, StartupContext context)
        {
            services.AddSingleton<IDataStore, RecordStore>();
        }

        public void ConfigureWebAppPipeline(IApplicationBuilder app, IServiceProvider appServices, StartupContext context)
        {
            
        }
    }
}