using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Controllers
{
    public class ManifestController : ControllerBase
    {
        private readonly IDataStore _dataStore;

        public ManifestController(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpGet]
        [HttpHead]
        public ActionResult Get(string repo, string reference)
        {
            var imageRepo = _dataStore.GetRepository(repo);
            if (imageRepo == null)
            {
                return new NotFoundResult();
            }

            var image = GetImageByReference(reference, imageRepo.Name);
            if (image == null)
            {
                return new NotFoundResult();
            }

            HttpContext.Response.Headers.Add("Docker-Content-Digest", image.DigestString);
            
            var manifestBytes = image.ManifestBytes;
            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (writeFile)
            {
                return new FileContentResult(manifestBytes, MediaTypeHeaderValue.Parse(image.ManifestMediaType));
            }

            HttpContext.Response.Headers.Add("Content-Type", image.ManifestMediaType);
            HttpContext.Response.Headers.Add("Content-Length", manifestBytes.Length.ToString());
            return new EmptyResult();
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpPut]
        public IActionResult Update(string repo, string reference)
        {
            using var memoryStream = new MemoryStream();
            Request.Body.CopyTo(memoryStream);
            var manifestBytes = memoryStream.ToArray();

            Manifest manifest = null;
            try
            {
                manifest = Manifest.Parse(manifestBytes);
            }
            catch
            {
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var existingImage = GetImageByReference(reference, repo);
            if (existingImage == null)
            {
                var image = new Image
                {
                    Tag   = manifest.Tag,
                    RepositoryName = repo,
                    ManifestBytes = manifest.RawJsonBytes
                };
                _dataStore.CreateImage(image);
            }
            else
            {
                existingImage.ManifestBytes = manifestBytes;
                _dataStore.UpdateImage(existingImage);
            }
            
            var digest = manifest.CalculateDigest();
            HttpContext.Response.Headers.Add("Location", $"/v2/{repo}/manifests/{reference}");
            HttpContext.Response.Headers.Add("Docker-Content-Digest", digest.GetHashString());
            return new StatusCodeResult((int) HttpStatusCode.Created);
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpDelete]
        public IActionResult Delete(string repo, string reference)
        {
            var imageRepo = _dataStore.GetRepository(repo);
            if (imageRepo == null)
            {
                return new NotFoundResult();
            }

            var image = GetImageByReference(reference, imageRepo.Name);
            if (image == null)
            {
                return new NotFoundResult();
            }

            _dataStore.DeleteImage(image);
            // todo: delete all orphan blobs...
            return new StatusCodeResult((int)HttpStatusCode.Accepted);
        }

        private Image GetImageByReference(string reference, string repoName)
        {
            return Digest.TryParse(reference, out _) 
                ? _dataStore.GetImagesByDigest(repoName, reference) 
                : _dataStore.GetImagesByTag(repoName, reference);
        }
    }
}