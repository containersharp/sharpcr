using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
                var manifest = JsonConvert.DeserializeObject<ManifestV2List>(Encoding.UTF8.GetString(jsonBytes));
                if (manifest.SchemaVersion != 2 || manifest.MediaType != GetAcceptableMediaTypes().Single())
                {
                    throw new NotSupportedException(
                        "Only version 2 schema version manifest lists are supported by this parser.");
                }

                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = Models.Digest.Compute( manifest.GetJsonBytesForComputingDigest() ).ToString();
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
        public class ManifestV2ListItem : Descriptor
        {
            public Platform Platform { get; set; }
        }

        public class Platform
        {
            public string Architecture { get; set; }

            public string Os { get; set; }
            [JsonProperty("os.version")]
            public string OsVersion { get; set; }
            [JsonProperty("os.features")]
            public string OsFeatures { get; set; }
            public string Variant { get; set; }

            public string[] Features { get; set; }
        }
    }
}