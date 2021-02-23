using System;
using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    public class ManifestController
    {

        [RegistryRoute("manifests/:{tag}", "manifests/@{manifest}")]
        [HttpGet]
        [HttpHead]
        public object Get(string tag, string manifest)
        {
            return null;
        }
        
        [RegistryRoute("manifests/:{tag}", "manifests/@{manifest}")]
        [HttpPut]
        public void Update(string tag, string manifest)
        {
            
        }
        
        [RegistryRoute("manifests/:{tag}", "manifests/@{manifest}")]
        [HttpDelete]
        public void Delete(string tag, string manifest)
        {
            
        }
    }
}