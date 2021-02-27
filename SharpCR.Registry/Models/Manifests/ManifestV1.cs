using System;
using System.Linq;
using System.Text.Json;

namespace SharpCR.Registry.Models.Manifests
{
    public class ManifestV1: Manifest
    {
      public string Name { get; private set; }

      public string Tag { get; private set; }
      
      public string Architecture { get; private set; }

      public ManifestV1(byte[] rawBytes)
      {
        RawJsonBytes = rawBytes;
      }

      public class Parser: IManifestParser
      {
        public string[] GetAcceptableMediaTypes()
        {
          return new string[]
          {
            "application/vnd.docker.distribution.manifest.v1+json",
            "application/vnd.docker.distribution.manifest.v1+prettyjws"
          };
        }
        // todo: application/vnd.oci.image.index.v1+json
        // todo: application/vnd.oci.image.manifest.v1+json
        public Manifest Parse(byte[] jsonBytes)
        {
          var manifestGlobalObject = JsonDocument.Parse(jsonBytes).RootElement;
          var schemaVersion = manifestGlobalObject.GetProperty("schemaVersion").GetInt32();
          if (schemaVersion > 1)
          {
            throw new NotSupportedException("Only version 1 schema version manifests are supported by this parser.");
          }
      
          var manifest = new ManifestV1(jsonBytes)
          {
            Layers = new Entity[0],
            Name = manifestGlobalObject.GetProperty("name").GetString(),
            Tag = manifestGlobalObject.GetProperty("tag").GetString(),
            Architecture = manifestGlobalObject.GetProperty("architecture").GetString()
          };

          var signature = manifestGlobalObject.TryGetProperty("signatures", out var signatureProp) ? signatureProp.GetString() : null;
          manifest.MediaType = "application/vnd.docker.distribution.manifest.v1+" +  (signature == null ? "json" : "prettyjws");
          if (manifestGlobalObject.TryGetProperty("fsLayers", out var layersArray)  
              && layersArray.ValueKind == JsonValueKind.Array)
          {
            var layers = new Entity[layersArray.GetArrayLength()];
            for (var index = 0; index < layers.Length; ++index)
            {
              var layerObj = layersArray[index];
              var blobSum = layerObj.GetProperty("blobSum").GetString();
              Models.Digest.TryParse(blobSum, out _);
              layers[index] = new Entity { MediaType  = "application/vnd.docker.image.rootfs.diff.tar.gzip", Digest = blobSum};
            }

            manifest.Layers = layers;
          }

          manifest.Digest = manifest.CalculateDigest().GetHashString();
          return manifest;
        }

      }
      
    }
}