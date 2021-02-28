using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: InternalsVisibleTo("SharpCR.Registry.Tests")]
namespace SharpCR.Registry.Features
{
    public class StartupContext
    {
        public IWebHostEnvironment HostEnvironment { get; internal set; }
        public IConfiguration Configuration  { get; internal set; }
    }
}