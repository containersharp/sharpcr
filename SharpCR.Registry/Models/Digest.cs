using System;
using System.IO;
using System.Linq;
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

    public static Digest Compute(byte[] bytes, HashAlgorithm hashAlgorithm = HashAlgorithm.SHA256)
    {
      using var ms = new MemoryStream(bytes);
      return Compute(ms, hashAlgorithm);
    }
    
    public static Digest Compute(Stream inputStream, HashAlgorithm hashAlgorithm = HashAlgorithm.SHA256)
    {
      var algName = hashAlgorithm.ToString();
      using var algorithm = System.Security.Cryptography.HashAlgorithm.Create(algName);
      return new Digest(algName.ToLower(), algorithm!.ComputeHash(inputStream));
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Digest otherDigest))
      {
        return false;
      }

      return string.Equals(this.Algorithm, otherDigest.Algorithm, StringComparison.OrdinalIgnoreCase) 
             && this._hashBytes.SequenceEqual(otherDigest._hashBytes);
    }

    public override int GetHashCode()
    {
      return BitConverter.ToInt32(this._hashBytes, 0);
    }
    
    

    public enum HashAlgorithm
    {
      // ReSharper disable InconsistentNaming
      SHA256,
      SHA512
    }

  }
}