namespace SharpCR.Registry.Records
{
    public class ArtifactRecord
    {
        public string DigestString { get; set; }
        public string RepositoryName { get; set; }
        public string Tag { get; set; }
        public byte[] ManifestBytes { get; set; }
        public string ManifestMediaType { get; set; }
    }
}