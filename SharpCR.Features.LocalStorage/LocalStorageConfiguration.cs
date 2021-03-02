using System;
using System.IO;
using System.Reflection;

namespace SharpCR.Features.LocalStorage
{
    public class LocalStorageConfiguration
    {
        public string BasePath { get; set; } 
        public string RecordsFileName { get; set; } = "records.json";
        public string BlobsDirectoryName { get; set; } = "blobs";
    }
}