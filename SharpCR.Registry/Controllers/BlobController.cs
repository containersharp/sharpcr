using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace SharpCR.Registry.Controllers
{
    public class BlobController : ControllerBase
    {
        private readonly IRecordStore _recordStore;
        private readonly IBlobStorage _blobStorage;

        public BlobController(IRecordStore recordStore, IBlobStorage blobStorage)
        {
            _recordStore = recordStore;
            _blobStorage = blobStorage;
        }

        [RegistryRoute("blobs/{digest}")]
        [HttpGet]
        [HttpHead]
        public ActionResult Get(string repo, string digest)
        {
            var blob = _recordStore.GetBlobByDigest(repo, digest);
            if (blob == null)
            {
                return new NotFoundResult();
            }

            HttpContext.Response.Headers.Add("Docker-Content-Digest", blob.DigestString);
            HttpContext.Response.Headers.Add("Content-Length", blob.ContentLength.ToString());

            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (writeFile)
            {
                var content = _blobStorage.GetByDigest(blob.Url);
                return new FileStreamResult(content, "application/octet-stream");
            }

            return new EmptyResult();
        }

        [RegistryRoute("blobs/uploads")]
        [HttpPost]
        public object CreateUpload([FromQuery] string digest, [FromQuery] string mount, [FromQuery] string @from)
        {
            return null;
        }

        [RegistryRoute("blobs/uploads/{reference}")]
        [HttpPatch]
        [HttpPut]
        public object ContinueUpload(string reference, [FromQuery] string digest)
        {
            return null;
        }

        [RegistryRoute("blobs/{digest}")]
        [HttpDelete]
        public ActionResult Delete(string repo, string digest)
        {
            var blob = _recordStore.GetBlobByDigest(repo, digest);
            if (blob == null)
            {
                return new NotFoundResult();
            }

            _recordStore.DeleteBlob(blob);
            _blobStorage.DeleteByDigest(blob.Url);

            return new StatusCodeResult((int) HttpStatusCode.Accepted);
        }
    }
}
