using System;
using System.IO;
using System.Reflection;

namespace SharpCR.Features.LocalStorage
{
    public class LocalStorageConfiguration
    {
        public string BasePath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public string FileName { get; set; } = "registry.json";
    }
}