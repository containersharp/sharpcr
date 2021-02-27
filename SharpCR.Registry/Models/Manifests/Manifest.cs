
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharpCR.Registry.Models.Manifests
{
    /// <summary>
    /// Represent a container manifest
    /// </summary>
    /// <remarks>
    /// There should be multiple implementations of this class.
    /// Please see OCI doc for Manifest: https://github.com/opencontainers/image-spec/blob/master/manifest.md
    /// </remarks>
    public abstract class Manifest : Descriptor
    {
        public int? SchemaVersion { get; set; }
        public Descriptor[] Layers { get; set; }
        
        [JsonIgnore]
        public byte[] RawJsonBytes { get; protected set; }

      protected virtual byte[] GetJsonBytesForComputingDigest()
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
    }
}