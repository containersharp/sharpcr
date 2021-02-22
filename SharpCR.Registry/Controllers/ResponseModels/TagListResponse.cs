using System.Collections.Generic;

namespace SharpCR.Registry.Controllers.ResponseModels
{
    // ReSharper disable InconsistentNaming
    public class TagListResponse
    {
        public string name { get; set; }
        public List<string> tags { get; set; }
    }
}