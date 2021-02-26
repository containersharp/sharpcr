using System;
using System.Text.Json;

namespace SharpCR.Registry.Models
{
    public class ImageLayer
    {

        public ImageLayer(string contentType, Digest digest)
        {
        
        }

        public static ImageLayer Parse(JsonElement jsonElement)
        {
            throw new NotImplementedException();
        }
    }
}