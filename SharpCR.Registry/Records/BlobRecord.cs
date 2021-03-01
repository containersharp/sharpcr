namespace SharpCR.Registry.Records
{
    public class BlobRecord
    {
        public string RepositoryName { get; set; }
        public string DigestString { get; set; }
        public long ContentLength { get; set; }
        public string Url { get; set; }
    }
}
