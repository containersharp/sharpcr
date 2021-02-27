using System;
using System.Linq;
using System.Text.Json;

namespace SharpCR.Registry.Models.Manifests
{
    public class ManifestV2: Manifest
    {
        public ManifestV2(byte[] rawBytes)
        {
            RawJsonBytes = rawBytes;
        }
        
        public ManifestV2()
        {
        }
        
        public Entity Config { get; set; }
        
        public class Parser : IManifestParser
        {
            public Manifest Parse(byte[] jsonBytes)
            {
                var manifestGlobalObject = JsonDocument.Parse(jsonBytes).RootElement;
                var schemaVersion = manifestGlobalObject.GetProperty("schemaVersion").GetInt32();
                var mediaType = manifestGlobalObject.GetProperty("mediaType").GetString();
                if (schemaVersion != 2 || mediaType != this.GetAcceptableMediaTypes().Single())
                {
                    throw new NotSupportedException(
                        "Only single version 2 schema version manifest is supported by this parser.");
                }

                var manifest = JsonSerializer.Deserialize<ManifestV2>(jsonBytes);
                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = manifest.CalculateDigest().GetHashString();
                return manifest;
            }

            public string[] GetAcceptableMediaTypes()
            {
                return new[]
                {
                    "application/vnd.docker.distribution.manifest.v2+json"
                };
            }
        }
    }
}