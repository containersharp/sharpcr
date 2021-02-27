using System.Collections.Generic;
using Newtonsoft.Json;

namespace SharpCR.Registry.Models.Manifests
{
    public class Entity
    {
        /// <summary>
        /// Content type of the entity
        /// </summary>
        /// <remarks>
        /// See source code at https://github.com/opencontainers/image-spec/blob/master/specs-go/v1/mediatype.go
        /// </remarks>
        public string MediaType { get; set; }
        
        public int? Size { get; set; }
        
        public string Digest { get; set; }
        
        public Dictionary<string, string> Annotations { get; set; }
    }
}