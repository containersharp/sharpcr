namespace SharpCR.Features.Records
{
    public class BlobRecord
    {
        public string RepositoryName { get; set; }
        public string DigestString { get; set; }
        public long ContentLength { get; set; }
        
        public string MediaType { get; set; }
        public string StorageLocation { get; set; }
    }
}
