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
        private readonly IBlobStorage _blobStorage;
        private readonly Lazy<Dictionary<string, IManifestParser>> _manifestParsers;

        public ManifestController(IDataStore dataStore, IBlobStorage blobStorage)
        {
            _dataStore = dataStore;
            _blobStorage = blobStorage;
            _manifestParsers = new Lazy<Dictionary<string, IManifestParser>>(InitializeManifestParsers);
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpGet]
        [HttpHead]
        public ActionResult Get(string repo, string reference)
        {
            var image = GetImageByReference(reference, repo);
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
            
            var mediaType = Request.Headers["Content-Type"];
            if (!_manifestParsers.Value.TryGetValue(mediaType.ToString(), out var acceptableParser))
            {
                // unsupported media type
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }
            
            using var memoryStream = new MemoryStream();
            Request.Body.CopyTo(memoryStream);
            var manifestBytes = memoryStream.ToArray();
            var manifest = acceptableParser.Parse(manifestBytes);
            var pushedDigest  = manifest.Digest;
            if (!string.IsNullOrEmpty(queriedDigest)  && !string.Equals(queriedDigest, pushedDigest, StringComparison.Ordinal))
            {
                // digest does not match in URL and body
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var referencedItems = manifest.GetReferencedDescriptors();
            if(referencedItems.Any(item => !ReferenceExists(item, repo)))
            {
                // some of the referenced item does not exist
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }
            
            var existingImage = queriedTag != null ?  _dataStore.GetImageByTag(repo, queriedTag) : null;
            if (existingImage == null)
            {   
                var image = new ImageRecord
                {
                    RepositoryName = repo,
                    Tag = queriedTag,
                    DigestString = pushedDigest,
                    ManifestBytes = manifest.RawJsonBytes,
                    ManifestMediaType = manifest.MediaType
                };
                _dataStore.CreateImage(image);
            }
            else
            {
                existingImage.DigestString = pushedDigest;
                existingImage.ManifestBytes = manifestBytes;
                existingImage.ManifestMediaType = manifest.MediaType;
                _dataStore.UpdateImage(existingImage);
                // todo: cleanup replaced blobs...
            }
            
            HttpContext.Response.Headers.Add("Location", $"/v2/{repo}/manifests/{reference}");
            HttpContext.Response.Headers.Add("Docker-Content-Digest", pushedDigest);
            return new StatusCodeResult((int) HttpStatusCode.Created);
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpDelete]
        public IActionResult Delete(string repo, string reference)
        {
            var image = GetImageByReference(reference, repo);
            if (image == null)
            {
                return new NotFoundResult();
            }

            _dataStore.DeleteImage(image);
            // todo: delete all orphan blobs...
            return new StatusCodeResult((int)HttpStatusCode.Accepted);
        }

        private static Dictionary<string, IManifestParser> InitializeManifestParsers()
        {
            var parsers = new Lazy<IManifestParser[]>(() => new IManifestParser[]
            {
                new ManifestV2.Parser(),
                new ManifestV1.Parser(),
                new ManifestV2List.Parser()
            });

            return parsers.Value.Select(p => Tuple.Create(p, p.GetAcceptableMediaTypes()))
                .SelectMany(x => x.Item2.Select(type => Tuple.Create(type, x.Item1)))
                .ToDictionary(i => i.Item1, i => i.Item2);
        }

        private bool ReferenceExists(Descriptor referencedItem, string repoName)
        {
            if (_manifestParsers.Value.ContainsKey(referencedItem.MediaType))
            {
                return (null != _dataStore.GetImageByDigest(repoName, referencedItem.Digest));
            }

            return _blobStorage.BlobExists(referencedItem);
        }

        private ImageRecord GetImageByReference(string reference, string repoName)
        {
            return Digest.TryParse(reference, out _) 
                ? _dataStore.GetImageByDigest(repoName, reference) 
                : _dataStore.GetImageByTag(repoName, reference);
        }
    }
}