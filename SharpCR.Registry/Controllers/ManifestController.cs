using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharpCR.Features;
using SharpCR.Features.Records;
using SharpCR.Manifests;


namespace SharpCR.Registry.Controllers
{
    public class ManifestController : ControllerBase
    {
        // todo: add logging
        // todo: handling errors
        private readonly IRecordStore _recordStore;
        private readonly Lazy<Dictionary<string, IManifestParser>> _manifestParsers;

        public ManifestController(IRecordStore recordStore)
        {
            _recordStore = recordStore;
            _manifestParsers = new Lazy<Dictionary<string, IManifestParser>>(InitializeManifestParsers);
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpGet]
        [HttpHead]
        public async Task<ActionResult> Get(string repo, string reference)
        {
            var artifact = await GetArtifactByReferenceAsync(reference, repo);
            if (artifact == null)
            {
                return new NotFoundResult();
            }

            HttpContext.Response.Headers.Add("Docker-Content-Digest", artifact.DigestString);

            var manifestBytes = artifact.ManifestBytes;
            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (writeFile)
            {
                return new FileContentResult(manifestBytes, MediaTypeHeaderValue.Parse(artifact.ManifestMediaType));
            }

            HttpContext.Response.Headers.Add("Content-Type", artifact.ManifestMediaType);
            HttpContext.Response.Headers.Add("Content-Length", manifestBytes.Length.ToString());
            return new EmptyResult();
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpPut]
        public async Task<IActionResult> Save(string repo, string reference)
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

            await using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var manifestBytes = memoryStream.ToArray();
            var manifest = acceptableParser.Parse(manifestBytes);
            var pushedDigest  = manifest.Digest;
            if (!string.IsNullOrEmpty(queriedDigest)  && !string.Equals(queriedDigest, pushedDigest, StringComparison.Ordinal))
            {
                // digest does not match in URL and body
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var referencedItems = manifest.GetReferencedDescriptors();
            foreach (var item in referencedItems)
            {
                if (!await ReferenceExistsAsync(item, repo))
                {
                    // some of the referenced item does not exist
                    return new StatusCodeResult((int) HttpStatusCode.BadRequest);    
                }
            }

            var existingArtifact = queriedTag != null ?  await _recordStore.GetArtifactByTagAsync(repo, queriedTag) : null;
            if (existingArtifact == null)
            {
                var artifact = new ArtifactRecord
                {
                    RepositoryName = repo,
                    Tag = queriedTag,
                    DigestString = pushedDigest,
                    ManifestBytes = manifest.RawJsonBytes,
                    ManifestMediaType = manifest.MediaType
                };
                await _recordStore.CreateArtifactAsync(artifact);
            }
            else
            {
                existingArtifact.DigestString = pushedDigest;
                existingArtifact.ManifestBytes = manifestBytes;
                existingArtifact.ManifestMediaType = manifest.MediaType;
                await _recordStore.UpdateArtifactAsync(existingArtifact);
                // todo: cleanup replaced blobs...
            }

            HttpContext.Response.Headers.Add("Location", $"/v2/{repo}/manifests/{reference}");
            HttpContext.Response.Headers.Add("Docker-Content-Digest", pushedDigest);
            return new StatusCodeResult((int) HttpStatusCode.Created);
        }

        [RegistryRoute("manifests/{reference}")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string repo, string reference)
        {
            var artifact = await GetArtifactByReferenceAsync(reference, repo);
            if (artifact == null)
            {
                return new NotFoundResult();
            }

            await _recordStore.DeleteArtifactAsync(artifact);
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

        private async Task<bool> ReferenceExistsAsync(Descriptor referencedItem, string repoName)
        {
            if (_manifestParsers.Value.ContainsKey(referencedItem.MediaType))
            {
                return (null != await _recordStore.GetArtifactByDigestAsync(repoName, referencedItem.Digest));
            }

            return (null != await _recordStore.GetBlobByDigestAsync(repoName, referencedItem.Digest));
        }

        private async Task<ArtifactRecord> GetArtifactByReferenceAsync(string reference, string repoName)
        {
            return Digest.TryParse(reference, out _)
                ? await _recordStore.GetArtifactByDigestAsync(repoName, reference)
                : await _recordStore.GetArtifactByTagAsync(repoName, reference);
        }
    }
}
