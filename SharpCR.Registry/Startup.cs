using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharpCR.Features;
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
                var baseProbe = "^/v2/?$";
                var baseCatalog = "^/v2/_catalog/?$";
                var manifestOperation = "^/v2/(?<repo>.+)/manifests/(?<reference>.+)$";
                var blobUploadContinue = "^/v2/(?<repo>.+)/blobs/uploads/(?<sessionId>.+)$";
                var blobUploadCreate = "^/v2/(?<repo>.+)/blobs/uploads/?$";
                var blobOperation = "^/v2/(?<repo>.+)/blobs/(?<digest>.+)$";
                var tagList = "^/v2/(?<repo>.+)/tags/list/?$";

        
                endpoints.MapControllerRoute(
                    name: "base_probe",
                    pattern: "/v2",
                    constraints: new
                    { 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    },
                    defaults: new { controller = "Base", action = "Get", });
                
                endpoints.MapControllerRoute(
                    name: "base_catalog",
                    pattern: "/v2/_catalog",
                    constraints: new
                    { 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    },
                    defaults: new { controller = "Base", action = "Get", });


                endpoints.MapControllerRoute(
                    name: "manifest_operations_get",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(manifestOperation), 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    },
                    defaults: new { controller = "Manifest", action = "Get", });

                endpoints.MapControllerRoute(
                    name: "manifest_operations_save",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(manifestOperation), 
                        httpMethod = new HttpMethodRouteConstraint("Put")
                    },
                    defaults: new { controller = "Manifest", action = "Delete", });
                endpoints.MapControllerRoute(
                    name: "manifest_operations_delete",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(manifestOperation), 
                        httpMethod = new HttpMethodRouteConstraint("Delete")
                    },
                    defaults: new { controller = "Manifest", action = "Delete", });


                endpoints.MapControllerRoute(
                    name: "blobs_upload_continue",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(blobUploadContinue), 
                        httpMethod = new HttpMethodRouteConstraint("Put", "Patch")
                    },
                    defaults: new { controller = "Blob", action = "ContinueUpload", });

                endpoints.MapControllerRoute(
                    name: "blobs_upload_create",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(blobUploadCreate), 
                        httpMethod = new HttpMethodRouteConstraint("Put", "Patch")
                    },
                    defaults: new { controller = "Blob", action = "CreateUpload", });

                endpoints.MapControllerRoute(
                    name: "blobs_operations_get",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(blobOperation), 
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    },
                    defaults: new { controller = "Blob", action = "Get", });

                endpoints.MapControllerRoute(
                    name: "blobs_operations_delete",
                    pattern: "{*url}",
                    constraints: new
                    {
                        url = new RegexNamedGroupRoutingConstraint(blobOperation), 
                        httpMethod = new HttpMethodRouteConstraint("Delete")
                    },
                    defaults: new { controller = "Blob", action = "Delete", });
                
                endpoints.MapControllerRoute(
                    name: "tag_list",
                    pattern: "{*url}",
                    constraints: new
                    { 
                        url = new RegexNamedGroupRoutingConstraint(tagList),
                        httpMethod = new HttpMethodRouteConstraint("Get", "Head")
                    },
                    defaults: new { controller = "Tag", action = "List", });
            });
        }
    }
}

// todo: Use serilog
