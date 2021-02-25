using System;

namespace SharpCR.Registry.Models
{
    public class Tag
    {
        public Guid RepositoryId { get; set; }
        public string Name { get; set; }
        
        public string DigestString { get; set; }
    }
}