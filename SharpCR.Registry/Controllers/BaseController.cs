using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    public class BaseController : ControllerBase
    {
        public ActionResult Base()
        {
            HttpContext.Response.Headers.Add("Docker-Distribution-API-Version", "registry/2.0");
            HttpContext.Response.Headers.Add("Vary", "Authorization");

            return new OkResult();
        }
        
        public object Catalog()
        {
            throw new System.NotImplementedException();
        }
    }
}