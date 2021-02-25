using System;
using System.Text;

namespace SharpCR.Registry.Models
{
   public sealed class Digest
  {
    private readonly byte[] _hashBytes;
    public Digest(string algorithm, byte[] hashBytes)
    {
      _hashBytes = hashBytes ?? throw new ArgumentNullException(nameof (hashBytes));
      Algorithm = algorithm;
    }
    
    public string Algorithm { get; }

    public string GetHashString() => HexUtility.ToString(_hashBytes); 

    // public bool IsSHA256 => string.Equals(this.Algorithm, "sha256", StringComparison.OrdinalIgnoreCase) || string.Equals(this.Algorithm, "tarsum.v1+sha256", StringComparison.OrdinalIgnoreCase);


    public static bool TryParse(string str, out Digest digest)
    {
      digest = null;
      if (string.IsNullOrEmpty(str))
      {
        return false;
      }

      var strArray = str.Split(new char[1]{ ':' }, 2);
      if (strArray.Length != 2 || strArray[1].Length % 2 != 0)
      {
        return false;
      }

      digest = new Digest(strArray[0], HexUtility.FromString(strArray[1]));
      return true;
    }

    public override string ToString()
    {
      var builder = new StringBuilder(Algorithm);
      
      builder.Append(':');
      builder.Append(GetHashString());
      
      return builder.ToString();
    }

  }
}