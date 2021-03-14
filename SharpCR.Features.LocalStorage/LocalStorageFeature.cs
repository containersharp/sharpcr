using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpCR.Registry;

namespace SharpCR.Features.LocalStorage
{
    public class LocalStorageFeature : IFeature
    {
        public void ConfigureServices(IServiceCollection services, StartupContext context)
        {
            var configuration = context.Configuration.GetSection("Features:LocalStorage")?.Get<LocalStorageConfiguration>() ?? new LocalStorageConfiguration();

            if (configuration.RecordStoreEnabled == true)
            {
                services.AddSingleton<IRecordStore, DiskRecordStore>();
            }

            if (configuration.BlobStorageEnabled == true)
            {
                services.AddSingleton<IBlobStorage, DiskBlobStorage>();
            }
        }

        public void ConfigureWebAppPipeline(IApplicationBuilder app, IServiceProvider appServices, StartupContext context)
        {
            
        }
    }
}