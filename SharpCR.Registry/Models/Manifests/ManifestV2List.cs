using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpCR.Registry.Models.Manifests
{
    public class ManifestV2List : Manifest
    {
        public ManifestV2List(byte[] rawBytes)
        {
            RawJsonBytes = rawBytes;
        }
        
        public ManifestV2List()
        {
        }
        
        public ManifestV2ListItem[] Manifests { get; set; }


        public class Parser: IManifestParser
        {
            public Manifest Parse(byte[] jsonBytes)
            {
                var manifestGlobalObject = JsonDocument.Parse(jsonBytes).RootElement;
                var schemaVersion = manifestGlobalObject.GetProperty("schemaVersion").GetInt32();
                var mediaType = manifestGlobalObject.GetProperty("mediaType").GetString();

                if (schemaVersion != 2 || mediaType != this.GetAcceptableMediaTypes().Single())
                {
                    throw new NotSupportedException(
                        "Only version 2 schema version manifest lists are supported by this parser.");
                }

                var manifest = JsonSerializer.Deserialize<ManifestV2List>(jsonBytes);
                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = manifest.CalculateDigest().GetHashString();
                return manifest;
            }

            public string[] GetAcceptableMediaTypes()
            {
                return new[]
                {
                    "application/vnd.docker.distribution.manifest.list.v2+json"
                };
            }
        }
        public class ManifestV2ListItem : Entity
        {
            public Platform Platform { get; set; }
        }

        public class Platform
        {
            public string Architecture { get; set; }

            public string Os { get; set; }
            [JsonPropertyName("os.version")]
            public string OsVersion { get; set; }
            [JsonPropertyName("os.features")]
            public string OsFeatures { get; set; }
            public string Variant { get; set; }

            public string[] Features { get; set; }
        }
    }
}