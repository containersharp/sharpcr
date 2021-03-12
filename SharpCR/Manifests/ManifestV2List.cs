using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SharpCR.Manifests
{
    /// <summary>
    /// Represent a manifest-list-type of manifest. That is also an OCI image index.
    /// </summary>
    /// <remarks>
    /// Docker documentation: https://docs.docker.com/registry/spec/manifest-v2-2/#manifest-list
    /// OCI specification: https://github.com/opencontainers/image-spec/blob/v1.0.1/image-index.md
    /// </remarks>
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


        public override Descriptor[] GetReferencedDescriptors()
        {
            return Manifests;
        }

        public class Parser: IManifestParser
        {
            public Manifest Parse(byte[] jsonBytes)
            {
                var manifest = JsonConvert.DeserializeObject<ManifestV2List>(Encoding.UTF8.GetString(jsonBytes));
                if (manifest.SchemaVersion != 2 || !GetAcceptableMediaTypes().Contains(manifest.MediaType))
                {
                    throw new NotSupportedException(
                        "Only version 2 schema version manifest lists are supported by this parser.");
                }

                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = SharpCR.Digest.Compute( manifest.GetJsonBytesForComputingDigest() ).ToString();
                return manifest;
            }

            public string[] GetAcceptableMediaTypes()
            {
                return new[]
                {
                    WellKnownMediaTypes.DockerManifestV2List,
                    WellKnownMediaTypes.OciImageIndexV1
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