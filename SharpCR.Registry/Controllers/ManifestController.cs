using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharpCR.Registry.Models;
using SharpCR.Registry.Models.Manifests;
using SharpCR.Registry.Records;

namespace SharpCR.Registry.Controllers
{
    public class ManifestController : ControllerBase
    {
        // todo: add logging
        // todo: handling errors
        private readonly IDataStore _dataStore;
        private Lazy<IManifestParser[]> _parsers;

        public ManifestController(IDataStore dataStore)
        {
            _dataStore = dataStore;
            _parsers = new Lazy<IManifestParser[]>(() => new IManifestParser[]
            {
                new ManifestV2.Parser(),
                new ManifestV1.Parser(),
                new ManifestV2List.Parser()
            });
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
        public IActionResult Save(string repo, string reference)
        {   
            string queriedTag = null;
            string queriedDigest = null;
            if (Digest.TryParse(reference, out _))
            {
                queriedDigest = reference;
            }
            else
            {
                queriedTag = reference;
            }
            
            using var memoryStream = new MemoryStream();
            Request.Body.CopyTo(memoryStream);
            var mediaType = Request.Headers["Content-Type"];
            var manifestBytes = memoryStream.ToArray();

            var acceptableParser = _parsers.Value.FirstOrDefault(p => p.GetAcceptableMediaTypes().Contains(mediaType.ToString()));
            if (acceptableParser == null)
            {
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var manifest = acceptableParser.Parse(manifestBytes);
            var pushedDigest  = manifest.Digest;
            
            if (!string.IsNullOrEmpty(queriedDigest) 
                && !string.IsNullOrEmpty(manifest.Digest) 
                && !string.Equals(queriedDigest, pushedDigest, StringComparison.Ordinal))
            {
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var existingImage = queriedTag != null ?  _dataStore.GetImagesByTag(repo, queriedTag) : null;
            if (existingImage == null)
            {
                var existingRepository = _dataStore.GetRepository(repo);
                if (existingRepository == null)
                {
                    _dataStore.CreateRepository(repo);
                }
                
                var image = new ImageRecord
                {
                    RepositoryName = repo,
                    Tag = queriedTag,
                    DigestString = pushedDigest,
                    ManifestBytes = manifest.RawJsonBytes
                };
                _dataStore.CreateImage(image);
            }
            else
            {
                existingImage.DigestString = pushedDigest;
                existingImage.ManifestBytes = manifestBytes;
                _dataStore.UpdateImage(existingImage);
            }
            // todo: check all the blobs are well received.
            
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

        private ImageRecord GetImageByReference(string reference, string repoName)
        {
            return Digest.TryParse(reference, out _) 
                ? _dataStore.GetImagesByDigest(repoName, reference) 
                : _dataStore.GetImagesByTag(repoName, reference);
        }
    }
}