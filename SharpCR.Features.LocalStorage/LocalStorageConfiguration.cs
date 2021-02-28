using System.IO;
using System.Reflection;

namespace SharpCR.Features.LocalStorage
{
    public class LocalStorageConfiguration
    {
        public string BasePath { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public string FileName { get; set; } = "registry.json";
    }
}