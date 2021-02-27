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
      public override Descriptor[] GetReferencedDescriptors()
      {
        return Layers;
      }
      
      public class Parser: IManifestParser
      {
        public string[] GetAcceptableMediaTypes()
        {
          return new[]
          {
            WellKnownMediaTypes.DockerImageManifestV1,
            WellKnownMediaTypes.DockerImageManifestV1Signed
          };
        }

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
            Layers = new Descriptor[0],
            
            Name = (string)manifestGlobalObject.Property("name"),
            Tag = (string)manifestGlobalObject.Property("tag"),
            Architecture = (string)manifestGlobalObject.Property("architecture")
          };

          var signature = manifestGlobalObject.Property("signatures")?.Value;
          manifest.MediaType = signature == null ? WellKnownMediaTypes.DockerImageManifestV1 : WellKnownMediaTypes.DockerImageManifestV1Signed;
          var layersArray = (JArray) manifestGlobalObject.Property("fsLayers")?.Value;
          if (layersArray != null)
          {
            var layers = new Descriptor[layersArray.Count];
            for (var index = 0; index < layers.Length; ++index)
            {
              var layerObj = (JObject)(layersArray[index]);
              var blobSum = (string) (layerObj.Property("blobSum"));
              Models.Digest.TryParse(blobSum, out _);
              layers[index] = new Descriptor { MediaType  = WellKnownMediaTypes.DockerImageLayerXGTar, Digest = blobSum};
            }

            manifest.Layers = layers;
          }

          manifest.Digest = Models.Digest.Compute( manifest.GetJsonBytesForComputingDigest() ).ToString();
          return manifest;
        }
      }
    }

}