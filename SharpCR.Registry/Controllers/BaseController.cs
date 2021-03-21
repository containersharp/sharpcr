using System;
using System.Buffers.Text;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    [Route("v2")]
    public class BaseController : ControllerBase
    {
        [Route("")]
        public ActionResult Base()
        {
            HttpContext.Response.Headers.Add(
                "Docker-Distribution-API-Version",
                "registry/2.0");
            HttpContext.Response.Headers.Add("Vary", "Authorization");

            var token = HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                HttpContext.Response.Headers.Add(
                    "Www-Authenticate",
                    "Bearer realm=\"http://kubernetes.docker.internal:5000/v2/token\"");
                return new UnauthorizedResult();
            }

            Console.WriteLine("token: " + token);

            return new OkResult();
        }

        [Route("token")]
        public ActionResult Token()
        {
            var auth = HttpContext.Request.Headers["Authorization"].ToString();
            var strs = Encoding.UTF8.GetString(
                Convert
                    .FromBase64String(auth.Remove(0, "Basic ".Length))).Split(":");

            Console.WriteLine(
                "username: " + strs[0] + ", password: " + strs[1]);

            return new ObjectResult(
                new
                {
                    access_token = "eyJhbGciOiJFUzI1NiIsInR5",
                }
            );
        }

        [Route("_catalog")]
        public object Catalog()
        {
            throw new System.NotImplementedException();
        }
    }
}
