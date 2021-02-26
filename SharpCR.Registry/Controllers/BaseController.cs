using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    [Route("v2")]
    public class BaseController : ControllerBase
    {
        [Route("")]
        public ActionResult Base()
        {
            HttpContext.Response.Headers.Add("Docker-Distribution-API-Version", "registry/2.0");
            HttpContext.Response.Headers.Add("Vary", "Authorization");

            return new OkResult();
        }
        
        [Route("_catalog")]
        public object Catalog()
        {
            throw new System.NotImplementedException();
        }
    }
}