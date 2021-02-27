using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
          using var ms = new MemoryStream(jsonBytes, false);
          using var sReader = new StreamReader(ms, Encoding.UTF8);
          using var jsonTextReader = new JsonTextReader(sReader);
          
          var manifestGlobalObject = JObject.Load(jsonTextReader);
          var schemaVersion = (int)(manifestGlobalObject.Property("schemaVersion")!.Value);
          if (schemaVersion > 1)
          {
            throw new NotSupportedException("Only version 1 schema version manifests are supported by this parser.");
          }
      
          var manifest = new ManifestV1(jsonBytes)
          {
            Layers = new Entity[0],
            
            Name = (string)manifestGlobalObject.Property("name"),
            Tag = (string)manifestGlobalObject.Property("tag"),
            Architecture = (string)manifestGlobalObject.Property("architecture")
          };

          var signature = manifestGlobalObject.Property("signatures")?.Value;
          manifest.MediaType = "application/vnd.docker.distribution.manifest.v1+" +  (signature == null ? "json" : "prettyjws");
          var layersArray = (JArray) manifestGlobalObject.Property("fsLayers")?.Value;
          if (layersArray != null)
          {
            var layers = new Entity[layersArray.Count];
            for (var index = 0; index < layers.Length; ++index)
            {
              var layerObj = (JObject)(layersArray[index]);
              var blobSum = (string) (layerObj.Property("blobSum"));
              Models.Digest.TryParse(blobSum, out _);
              layers[index] = new Entity { MediaType  = "application/vnd.docker.container.image.rootfs.diff+x-gtar", Digest = blobSum};
            }

            manifest.Layers = layers;
          }

          manifest.Digest = manifest.ComputeDigest().ToString();
          return manifest;
        }

      }
      
    }

}