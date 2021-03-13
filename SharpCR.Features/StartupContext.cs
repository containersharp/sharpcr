using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: InternalsVisibleTo("SharpCR.Registry.Tests")]
[assembly: InternalsVisibleTo("SharpCR.Registry")]
namespace SharpCR.Features
{
    public class StartupContext
    {
        public IWebHostEnvironment HostEnvironment { get; internal set; }
        public IConfiguration Configuration  { get; internal set; }
    }
}