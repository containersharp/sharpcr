using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    public class BlobController
    {

        [RegistryRoute("blobs/{digest}")]
        [HttpGet]
        [HttpHead]
        public object Get(string digest)
        {
            return null;
        }
        
        [RegistryRoute("blobs/uploads")]
        [HttpPost]
        public object CreateUpload([FromQuery]string digest, [FromQuery]string mount, [FromQuery]string @from)
        {
            return null;
        }
        
        [RegistryRoute("blobs/uploads/{reference}")]
        [HttpPatch]
        [HttpPut]
        public object ContinueUpload(string reference, [FromQuery]string digest)
        {
            return null;
        }
        
        [RegistryRoute("blobs/{digest}")]
        [HttpDelete]
        public void Delete(string digest)
        {
            
        }

    }
}