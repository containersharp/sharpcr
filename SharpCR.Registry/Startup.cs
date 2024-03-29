using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Features;
using SharpCR.Registry.Controllers;
using SharpCR.Registry.FeatureLoading;

namespace SharpCR.Registry
{
    public class Startup
    {
        private readonly IReadOnlyList<IFeature> _loadedFeatures;
        private readonly StartupContext _context;

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _context = new StartupContext
            {
                Configuration = configuration,
                HostEnvironment = webHostEnvironment
            };
            
            _loadedFeatures = FeatureLoader.LoadFeatures(configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            var configuration = _context.Configuration.GetSection("Settings")?.Get<Settings>() ?? new Settings();
            services.AddSingleton(Options.Create(configuration));
            _loadedFeatures.ForEach(feature => feature.ConfigureServices(services, _context));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IServiceProvider services)
        {
            if (_context.HostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            
            _loadedFeatures.ForEach(feature => feature.ConfigureWebAppPipeline(app, services,  _context));
            app.UseEndpoints(endpoints =>
            {   
                endpoints.MapControllerRoute("base_probe",
                    "v2",
                    new { controller = "Base", action = "Base", }, 
                    new
                    { 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    });
                
                endpoints.MapControllerRoute("base_catalog",
                    "v2/_catalog",
                    new { controller = "Base", action = "Catalog", },
                    new
                    { 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    });

                endpoints.MapRegexRoutes<ManifestController>();
                endpoints.MapRegexRoutes<BlobController>();
                endpoints.MapRegexRoutes<TagController>();
            });
        }
    }
}

// todo: Use serilog
