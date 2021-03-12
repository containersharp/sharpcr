using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SharpCR.Manifests
{
    /// <summary>
    /// Represent an image manifest.
    /// Docker v2 schemaVersion is identical to OCI v1
    /// </summary>
    /// <remarks>
    /// Docker documentation: https://docs.docker.com/registry/spec/manifest-v2-2/
    /// OCI specification: https://github.com/opencontainers/image-spec/blob/v1.0.1/manifest.md
    /// </remarks>
    public class ManifestV2: Manifest
    {
        public ManifestV2(byte[] rawBytes)
        {
            RawJsonBytes = rawBytes;
        }

        public ManifestV2()
        {
        }

        public Descriptor Config { get; set; }

        public override Descriptor[] GetReferencedDescriptors()
        {
            return Config == null
                ? Layers
                : Layers.Concat(new[] {Config}).ToArray();
        }

        public class Parser : IManifestParser
        {
            public Manifest Parse(byte[] jsonBytes)
            {
                var manifest = JsonConvert.DeserializeObject<ManifestV2>(Encoding.UTF8.GetString(jsonBytes));
                if (manifest.SchemaVersion != 2 || !GetAcceptableMediaTypes().Contains(manifest.MediaType))
                {
                    throw new NotSupportedException(
                        "Only single version 2 schema version manifest is supported by this parser.");
                }

                // Tag could be included in annotation `org.opencontainers.image.ref.name`
                // https://github.com/opencontainers/image-spec/blob/master/image-layout.md
                manifest.RawJsonBytes = jsonBytes;
                manifest.Digest = SharpCR.Digest.Compute( manifest.GetJsonBytesForComputingDigest() ).ToString();
                return manifest;
            }

            public string[] GetAcceptableMediaTypes()
            {
                return new[]
                {
                    WellKnownMediaTypes.DockerImageManifestV2,
                    WellKnownMediaTypes.OciImageManifestV1
                };
            }
        }
    }
}
