using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SharpCR.Features;
using SharpCR.Features.Records;
using SharpCR.Manifests;


namespace SharpCR.Registry.Controllers
{
    /// <summary>
    /// Implements the manifests related APIs
    /// </summary>
    /// <remarks>
    /// Docker Distribution Registry Implementation
    /// https://github.com/distribution/distribution/blob/main/registry/handlers/manifests.go#L83
    /// </remarks>
    public class ManifestController : ControllerBase
    {
        // todo: add logging
        // todo: handling errors
        // todo: content negotiation and OCI image compliance
        
        private readonly IRecordStore _recordStore;
        private readonly ILogger<ManifestController> _logger;
        private readonly Lazy<Dictionary<string, IManifestParser>> _manifestParsers;
        private const string ManifestUrlPattern = "^v2/(?<repo>.+)/manifests/(?<reference>.+)$";

        public ManifestController(IRecordStore recordStore, ILogger<ManifestController> logger)
        {
            _recordStore = recordStore;
            _logger = logger;
            _manifestParsers = new Lazy<Dictionary<string, IManifestParser>>(InitializeManifestParsers);
        }

        [NamedRegexRoute(ManifestUrlPattern, "Get", "Head")]
        public async Task<ActionResult> Get(string repo, string reference)
        {
            var artifact = await GetArtifactByReferenceAsync(reference, repo);
            if (artifact == null)
            {
                _logger.LogDebug("Manifest not found {@query}", new {repo, reference});
                return new NotFoundResult();
            }

            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            var manifestBytes = artifact.ManifestBytes;
            
            HttpContext.Response.Headers.Add("Docker-Content-Digest", artifact.DigestString);
            HttpContext.Response.Headers.Add("Content-Type", artifact.ManifestMediaType);
            HttpContext.Response.Headers.Add("Content-Length", manifestBytes.Length.ToString());

            if (!writeFile)
            {
                _logger.LogDebug("Skipping writing content for HEAD request of manifest {@query}", new {repo, reference});
                return new EmptyResult();
            }

            _logger.LogInformation("Writing content for manifest {@query}", new {repo, reference, digest = artifact.DigestString});
            return new FileContentResult(manifestBytes, MediaTypeHeaderValue.Parse(artifact.ManifestMediaType));
        }

        [NamedRegexRoute(ManifestUrlPattern, "Put")]
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
                _logger.LogDebug("Unsupported media type received for manifest {@req}", new {repo, reference, digest = queriedDigest, mediaType});
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            await using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var manifestBytes = memoryStream.ToArray();
            var manifest = acceptableParser.Parse(manifestBytes);
            var pushedDigest  = manifest.Digest;
            if (!string.IsNullOrEmpty(queriedDigest)  && !string.Equals(queriedDigest, pushedDigest, StringComparison.Ordinal))
            {
                _logger.LogDebug("Digest in URL does not match that computed from payload: {@req}", new {repo, reference, queriedDigest, pushedDigest});
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            var referencedItems = manifest.GetReferencedDescriptors();
            foreach (var item in referencedItems)
            {
                if (!await ReferenceExistsAsync(item, repo))
                {
                    _logger.LogDebug("One or more referenced items was not pushed before this manifest is created: {@req}", 
                        new {repo, reference = item.Digest, mediaType = item.MediaType});
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

            _logger.LogInformation("New manifest created: {@req}", 
                new {repo, reference, digest = pushedDigest, mediaType = manifest.MediaType});
            HttpContext.Response.Headers.Add("Location", $"/v2/{repo}/manifests/{reference}");
            HttpContext.Response.Headers.Add("Docker-Content-Digest", pushedDigest);
            return new StatusCodeResult((int) HttpStatusCode.Created);
        }

        [NamedRegexRoute(ManifestUrlPattern, "Delete")]
        public async Task<IActionResult> Delete(string repo, string reference)
        {
            var artifact = await GetArtifactByReferenceAsync(reference, repo);
            if (artifact == null)
            {
                _logger.LogDebug("Manifest not found: {@req}", new {repo, reference});
                return new NotFoundResult();
            }

            await _recordStore.DeleteArtifactAsync(artifact);
            _logger.LogInformation("Manifest deleted: {@req}", new {repo, reference});
            return new StatusCodeResult((int)HttpStatusCode.Accepted);
            
            // todo: delete all orphan blobs...
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
                // this is a sub manifest
                return (await _recordStore.GetArtifactsByDigestAsync(repoName, referencedItem.Digest)).Any();
            }

            // this is a blob
            return (null != await _recordStore.GetBlobByDigestAsync(repoName, referencedItem.Digest));
        }

        private async Task<ArtifactRecord> GetArtifactByReferenceAsync(string reference, string repoName)
        {
            return Digest.TryParse(reference, out _)
                ? (await _recordStore.GetArtifactsByDigestAsync(repoName, reference)).FirstOrDefault()
                : await _recordStore.GetArtifactByTagAsync(repoName, reference);
        }
    }
}
