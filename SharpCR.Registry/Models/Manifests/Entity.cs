using System.Text.Json.Serialization;

namespace SharpCR.Registry.Models.Manifests
{
    public class Entity
    {
        public string MediaType { get; set; }
        
        public int Size { get; set; }
        public string Digest { get; set; }
        
        [JsonIgnore]
        public byte[] RawJsonBytes { get; protected set; }
    }
}