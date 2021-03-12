namespace SharpCR.Manifests
{
    public interface IManifestParser
    {
        public Manifest Parse(byte[] jsonBytes);
        public string[] GetAcceptableMediaTypes();

    }
}