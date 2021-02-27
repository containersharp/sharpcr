
using System.Security.Cryptography;
using System.Text.Json;

namespace SharpCR.Registry.Models.Manifests
{
    public abstract class Manifest : Entity
    {
        public int? SchemaVersion { get; set; }
        public Entity[] Layers { get; protected set; }

      protected virtual byte[] GetRawBytesWithoutSignature()
      {
          var manifestGlobalObject = JsonDocument.Parse(RawJsonBytes).RootElement;
          if (!manifestGlobalObject.TryGetProperty("signatures", out _))
          {
              return RawJsonBytes;
          }

          return new byte[0];
      }
      
      public Digest CalculateDigest()
      {
          using var sha256 = HashAlgorithm.Create("SHA256");
          return new Digest("sha256", sha256!.ComputeHash(this.GetRawBytesWithoutSignature()));
      }
    }
}