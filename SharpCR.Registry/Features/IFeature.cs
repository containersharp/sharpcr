using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCR.Registry.Features
{
    public interface IFeature
    {
        void ConfigureServices(IServiceCollection services, StartupContext context);
        void ConfigureWebAppPipeline(IApplicationBuilder app, IServiceProvider appServices, StartupContext context);
        
        
        // ILogger
        // IMessage
    }
}