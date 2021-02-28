using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace SharpCR.Registry.Features
{
    public class StartupContext
    {
        public IWebHostEnvironment WebHostEnvironment { get; internal set; }
        public IHostEnvironment HostEnvironment { get; internal set; }
        public IConfiguration Configuration  { get; internal set; }
    }
}