namespace SharpCR.Registry
{
    public class Settings
    {
        public string TemporaryFilesRootPath { get; set; } 

        public string BlobUploadSessionIdPrefix { get; set; } = "local_";
    }
}