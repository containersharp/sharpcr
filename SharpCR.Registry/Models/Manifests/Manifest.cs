
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpCR.Registry.Models.Manifests
{
    public abstract class Manifest : Entity
    {
        public int? SchemaVersion { get; set; }
        public Entity[] Layers { get; protected set; }

      protected virtual byte[] GetJsonBytesWithoutSignature()
      {
          using var inputStream = new MemoryStream(RawJsonBytes, false);
          using var sReader = new StreamReader(inputStream, Encoding.UTF8);
          using var jsonTextReader = new JsonTextReader(sReader);
          
          var manifestGlobalObject = JObject.Load(jsonTextReader);
          var signatures = manifestGlobalObject.Property("signatures");
          if (signatures == null)
          {
              return RawJsonBytes;
          }

          signatures.Remove();
          using var outputStream = new MemoryStream();
          using var sWriter = new StreamWriter(outputStream, Encoding.UTF8, 16, true) {NewLine = "\n"};
          using var jsonTextWriter = new JsonTextWriter(sWriter)
          {
              Indentation = 3, 
              Formatting = Formatting.Indented, 
              IndentChar = ' '
          };

          manifestGlobalObject.WriteTo(jsonTextWriter);
          return outputStream.ToArray();
      }
      
      public Digest ComputeDigest()
      {
          using var sha256 = HashAlgorithm.Create("SHA256");
          return new Digest("sha256", sha256!.ComputeHash(GetJsonBytesWithoutSignature()));
      }
    }
}