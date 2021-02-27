using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
                var manifest = JsonConvert.DeserializeObject<ManifestV2>(Encoding.UTF8.GetString(jsonBytes));
                if (manifest.SchemaVersion != 2 || manifest.MediaType != GetAcceptableMediaTypes().Single())
                {
                    throw new NotSupportedException(
                        "Only single version 2 schema version manifest is supported by this parser.");
                }

                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = manifest.ComputeDigest().ToString();
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