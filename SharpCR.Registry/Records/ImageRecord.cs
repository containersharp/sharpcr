namespace SharpCR.Registry.Records
{
    public class ImageRecord
    {
        public string DigestString { get; set; }
        public string RepositoryName { get; set; }
        public string Tag { get; set; }
        public byte[] ManifestBytes { get; set; }
        public string ManifestMediaType { get; set; }
    }
}