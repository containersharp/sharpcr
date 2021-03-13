using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpCR.Features;
using SharpCR.Registry.Features;

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
            _loadedFeatures.ForEach(feature => feature.ConfigureServices(services, _context));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IServiceProvider services)
        {
            if (_context.HostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            _loadedFeatures.ForEach(feature => feature.ConfigureWebAppPipeline(app, services,  _context));
        }
    }
}
