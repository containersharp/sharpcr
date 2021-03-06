using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SharpCR.Registry
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseKestrel(op =>
                        {
                            op.Limits.MaxRequestBodySize = 1L * 1024 * 1024 * 1024; // 1GB
                        });
                });
    }
}
