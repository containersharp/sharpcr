using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Controllers
{
    public class ManifestController : ControllerBase
    {
        private readonly IDataStore<ImageRepository> _imageRepositoryStore;
        private readonly IDataStore<Image> _imageStore;

        public ManifestController(IDataStore<ImageRepository> imageRepositoryStore, IDataStore<Image> imageStore)
        {
            _imageRepositoryStore = imageRepositoryStore;
            _imageStore = imageStore;
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpGet]
        [HttpHead]
        public ActionResult Get(string repo, string reference)
        {
            var imageRepo = _imageRepositoryStore.All()
                .FirstOrDefault(r => string.Equals(r.Name, repo, StringComparison.OrdinalIgnoreCase));
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
        public void Update(string reference)
        {
            
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpDelete]
        public void Delete(string reference)
        {
            
        }

        private Image GetImageByReference(string reference, string repoName)
        {
            if (Digest.TryParse(reference, out _))
            {
                return _imageStore.All().FirstOrDefault(t =>
                    string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.DigestString, reference, StringComparison.OrdinalIgnoreCase));
            }

            return _imageStore.All().FirstOrDefault(t =>
                string.Equals(t.RepositoryName, repoName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.Tag, reference, StringComparison.OrdinalIgnoreCase));
        }
    }
}