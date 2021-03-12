using System;
using System.Text;

namespace SharpCR.Registry.Models
{
    public class HexUtility
    {
        public static byte[] FromString(string hexString)
        {
            var hashBytes = new byte[hexString.Length / 2];
            
            for (var i = 0; i < hashBytes.Length; ++i)
            {
                var hex1 = FromChar(hexString[i * 2]);
                var hex2 = FromChar(hexString[i * 2 + 1]);
                hashBytes[i] = (byte) (hex1 << 4 | hex2);
            }

            return hashBytes;
        }
        public static string ToString(byte[] numbers)
        {
            var hashBuilder = new StringBuilder();
            foreach (var hashByte in numbers)
            {
                hashBuilder.Append(ToChar((byte) (hashByte >> 4 & 15)));
                hashBuilder.Append(ToChar((byte) (hashByte & 15)));
            }
            return hashBuilder.ToString();
        }
        
        private static byte FromChar(char c)
        {
            if (c >= '0' & c <= '9')
            {
                return (byte) (c - 48);
            }

            if (c >= 'a' & c <= 'f')
            {
                return (byte) (c - 97 + 10);
            }

            if (c >= 'A' & c <= 'F')
            {
                return (byte) (c - 65 + 10);
            }

            throw new FormatException("Invalid hex string");
        }

        private static char ToChar(byte hexNumber)
        {
            if (hexNumber <= 9)
                return (char) (48 + hexNumber);
      
            if (hexNumber >= 10 && hexNumber <= 15)
                return (char) (97 + hexNumber - 10);
      
            throw new FormatException($"Incorrect hex number {hexNumber}");
        }
    }
}