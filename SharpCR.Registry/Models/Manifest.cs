using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SharpCR.Registry.Models
{
    public class Manifest
    {
      
      public int SchemaVersion { get; private set; }

      public string Name { get; private set;}

      public string Tag { get; private set;}

      public string MediaType { get;private set; }

      public SubImage[] SubImages { get; private set; }
      
      public ImageLayer Config { get; private set;}
      
      public ImageLayer[] Layers { get; private set;}
      
      public byte[] RawJsonBytes { get; private set;}

      
      public Digest CalculateDigest()
      {
        using var sha256 = HashAlgorithm.Create("SHA256");
        return new Digest("sha256", sha256!.ComputeHash(this.GetRawBytesWithoutSignature()));
      }

      private byte[] GetRawBytesWithoutSignature()
      {
        var manifestGlobalObject = JsonDocument.Parse(RawJsonBytes).RootElement;
        if (!manifestGlobalObject.TryGetProperty("signatures", out _))
        {
          return RawJsonBytes;
        }

        return new byte[0];
      }

      public static Manifest Parse(byte[] jsonBytes)
      {
        var manifestGlobalObject = JsonDocument.Parse(jsonBytes).RootElement;
        var schemaVersion = manifestGlobalObject.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion > 2)
        {
          throw new NotSupportedException("Only version 1 or 2 schemaVersion are supported.");
        }
        
        var manifest = schemaVersion < 2 
          ? ParseV1Manifest(manifestGlobalObject) 
          : ParseV2Manifest(manifestGlobalObject);
        manifest.RawJsonBytes = jsonBytes;
        return manifest;
      }

      private static Manifest ParseV2Manifest(JsonElement manifestGlobalObject)
      {
        var manifest = new Manifest
        {
          Layers = new ImageLayer[0], 
          MediaType = manifestGlobalObject.GetProperty("mediaType").GetString()
        };

        if (manifest.MediaType == "application/vnd.docker.distribution.manifest.list.v2+json")
        {
          var manifests = manifestGlobalObject.GetProperty("manifests");
          manifest.SubImages = manifests.EnumerateArray().Select(SubImage.Parse).ToArray();
          return manifest;
        }

        if (manifestGlobalObject.TryGetProperty("config", out var configProp))
        {
          manifest.Config = ImageLayer.Parse(configProp);
        }

        if (manifestGlobalObject.TryGetProperty("layers", out var layersProp)
            && layersProp.ValueKind == JsonValueKind.Array
            && layersProp.GetArrayLength() > 0)
        {
          manifest.Layers = layersProp.EnumerateArray().Select(ImageLayer.Parse).ToArray();
        }

        return manifest;
      }

      private static Manifest ParseV1Manifest(JsonElement manifestGlobalObject)
      {
        var manifest = new Manifest
        {
          Layers = new ImageLayer[0],
          Name = manifestGlobalObject.GetProperty("name").GetString(),
          Tag = manifestGlobalObject.GetProperty("tag").GetString()
        };

        var signature = manifestGlobalObject.TryGetProperty("signatures", out var signatureProp) ? signatureProp.GetString() : null;
        manifest.MediaType = "application/vnd.docker.distribution.manifest.v1+" +  (signature == null ? "json" : "prettyjws");
        if (manifestGlobalObject.TryGetProperty("fsLayers", out var layersArray)  
            && layersArray.ValueKind == JsonValueKind.Array)
        {
          var layers = new ImageLayer[layersArray.GetArrayLength()];
          for (var index = 0; index < layers.Length; ++index)
          {
            var layerObj = layersArray[index];
            var sum = layerObj.GetProperty("blobSum").GetString();
            Digest.TryParse(sum, out var digest);
            layers[index] = new ImageLayer("application/vnd.docker.container.image.rootfs.diff+x-gtar", digest);
          }

          manifest.Layers = layers;
        }

        return manifest;
      }
    }
}