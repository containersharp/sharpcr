using System.Collections.Generic;

namespace SharpCR.Registry.Models
{
    public class Descriptor
    {
        /// <summary>
        /// Content type of the entity
        /// </summary>
        /// <remarks>
        /// See source code at https://github.com/opencontainers/image-spec/blob/master/specs-go/v1/mediatype.go
        /// </remarks>
        public string MediaType { get; set; }

        public long? Size { get; set; }

        public string Digest { get; set; }

        /// <summary>
        /// Annotations https://github.com/opencontainers/image-spec/blob/v1.0.1/annotations.md#pre-defined-annotation-keys
        /// </summary>
        public Dictionary<string, string> Annotations { get; set; }

        public string[] Urls { get; set; }
    }

    /// <summary>
    /// Many of the wellknown types defined by Docker and OCI
    /// </summary>
    /// <remarks>
    /// Docker doc at: https://docs.docker.com/registry/spec/manifest-v2-2/#media-types
    /// OCI doc at: https://github.com/opencontainers/image-spec/blob/master/media-types.md
    /// </remarks>
    public static class WellKnownMediaTypes
    {
        public const string DockerImageManifestV1 = "application/vnd.docker.distribution.manifest.v1+json";

        public const string DockerImageManifestV1Signed = "application/vnd.docker.distribution.manifest.v1+prettyjws";

        public const string DockerImageManifestV2 = "application/vnd.docker.distribution.manifest.v2+json";

        public const string DockerManifestV2List = "application/vnd.docker.distribution.manifest.list.v2+json";


        public const string DockerImageConfig = "application/vnd.docker.container.image.v1+json";

        public const string DockerImageLayerTarGzipped = "application/vnd.docker.image.rootfs.diff.tar.gzip";

        public const string DockerImageLayerXGTar = "application/vnd.docker.container.image.rootfs.diff+x-gtar";


        public const string OciImageManifestV1 = "application/vnd.oci.image.manifest.v1+json";

        public const string OciImageIndexV1 = "application/vnd.oci.image.index.v1+json";


        public const string OciImageConfig = "application/vnd.oci.image.config.v1+json";

        public const string OciImageLayer = "application/vnd.oci.image.layer.v1.tar";

        public const string OciImageLayerTarGzipped = "application/vnd.oci.image.layer.v1.tar+gzip";

        public const string OciImageLayerTarZstd = "application/vnd.oci.image.layer.v1.tar+zstd";
    }
}
