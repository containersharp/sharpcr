using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpCR.Features;
using SharpCR.Features.Records;

namespace SharpCR.Registry.Controllers
{
    /// <summary>
    /// Implements the blob related APIs
    /// </summary>
    /// <remarks>
    /// Docker Distribution Registry Implementation
    /// https://github.com/distribution/distribution/blob/main/registry/handlers/blob.go
    /// </remarks>
    public class BlobController : ControllerBase
    {
        private readonly IRecordStore _recordStore;
        private readonly IBlobStorage _blobStorage;
        private readonly Settings _settings;
        private readonly ILogger<BlobController> _logger;
        
        private const string BlobUploadContinueUrlPattern = "^v2/(?<repo>.+)/blobs/uploads/(?<sessionId>.+)$";
        private const string BlobUploadCreateUrlPattern = "^v2/(?<repo>.+)/blobs/uploads/?$";
        private const string BlobOperationUrlPattern = "^v2/(?<repo>.+)/blobs/(?<digest>.+)$";

        public BlobController(IRecordStore recordStore, IBlobStorage blobStorage, IOptions<Settings> settings, 
            IWebHostEnvironment environment, ILogger<BlobController> logger)
        {
            _recordStore = recordStore;
            _blobStorage = blobStorage;
            _logger = logger;
            _settings = settings.Value;
            _settings.TemporaryFilesRootPath ??= environment.ContentRootPath;
        }

        [NamedRegexRoute(BlobOperationUrlPattern, 3,"Get", "Head")]
        public async Task<IActionResult> Get(string repo, string digest)
        {
            var blob = await _recordStore.GetBlobByDigestAsync(repo, digest);
            if (blob == null)
            {
                _logger.LogDebug("Blob not found: {@blob}", new {repo, digest});
                return NotFound();
            }

            HttpContext.Response.Headers.Add("Docker-Content-Digest", blob.DigestString);
            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (!writeFile)
            {
                HttpContext.Response.Headers.Add("Content-Length", blob.ContentLength.ToString());
                _logger.LogDebug("Skipping writing content for HEAD request of blob {@blob}, storage location: {@blobLoc}", blob.DigestString, blob.StorageLocation);
                return new EmptyResult();
            }

            if (!_blobStorage.SupportsDownloading)
            {
                HttpContext.Response.Headers.Add("Content-Length", blob.ContentLength.ToString());
                _logger.LogInformation("Writing content for blob {@blob} from {@blobLoc}...", blob.DigestString, blob.StorageLocation);
                var content = await _blobStorage.ReadAsync(blob.StorageLocation);
                return new FileStreamResult(content, blob.MediaType ?? "application/octet-stream");
            }
            else
            {
                _logger.LogInformation("Redirecting to download url for blob {@blob}...", blob.DigestString);
                var downloadableUrl = await _blobStorage.GenerateDownloadUrlAsync(blob.StorageLocation);
                return Redirect(downloadableUrl);
            }
        }

        [NamedRegexRoute(BlobUploadCreateUrlPattern, 2,"Post")]
        [HttpPost]
        public async Task<IActionResult> CreateUpload(string repo, [FromQuery] string digest, [FromQuery] string mount, [FromQuery] string @from)
        {
            var sessionId = $"{_settings.BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";
            var monolithicUpload = !string.IsNullOrEmpty(digest);
            var isMount = !string.IsNullOrEmpty(mount);
            var chunkedEncoding = !Request.ContentLength.HasValue && Request.Headers["Transfer-Encoding"] == "chunked";

            if (monolithicUpload)
            {
                _logger.LogDebug("Receiving monolithic upload: {@session}...", new { repo, sessionId = (string)null});
                return await FinishUploading(repo, digest, new FileInfo(TempPathForUploadingBlob(sessionId)), chunkedEncoding, null);
            }
            else if (isMount)
            {
                _logger.LogDebug("Trying to mount blob from existing {@session}...", new { repo, digest, from});
                return await MountBlob(repo, mount, @from, sessionId);
            }
            else
            {
                return StartUploading(repo, sessionId);
            }
        }

        [NamedRegexRoute(BlobUploadContinueUrlPattern, 1, "Put", "Patch")]
        public async Task<IActionResult> ContinueUpload(string repo, string sessionId, [FromQuery] string digest)
        {
            var sessionIdPrefix = sessionId.Split("_", StringSplitOptions.RemoveEmptyEntries);
            if (sessionIdPrefix.Length != 2 || !string.Equals(sessionIdPrefix[0], _settings.BlobUploadSessionIdPrefix))
            {
                _logger.LogDebug("Bad session id: {@session}...", new { repo, sessionId, digest});
                return BadRequest();
            }

            var blobTempFile = new FileInfo(TempPathForUploadingBlob(sessionId));
            var receivingChunks = string.Equals(Request.Method, HttpMethod.Patch.ToString(), StringComparison.OrdinalIgnoreCase);
            var chunkedEncoding = !Request.ContentLength.HasValue && Request.Headers["Transfer-Encoding"] == "chunked";

            if (receivingChunks)
            {
                _logger.LogDebug("Receiving chunk in session {@session}...", new { repo, sessionId, digest});
                Response.Headers.Add("Docker-Upload-UUID", sessionId);
                var chunkSaveSucceeded = await SaveChunk(blobTempFile, chunkedEncoding, false, repo, sessionId, digest);
                var receivedSuccessfully = chunkSaveSucceeded.Item1;
                if (receivedSuccessfully)
                {
                    _logger.LogInformation("Blob chunk received in session {@session}...", new { repo, sessionId, digest});
                }

                return receivedSuccessfully 
                    ? Accepted($"/v2/{repo}/blobs/uploads/{sessionId}")
                    : chunkSaveSucceeded.Item2;
            }

            return await FinishUploading(repo, digest, blobTempFile, chunkedEncoding, sessionId);
        }

        [NamedRegexRoute(BlobOperationUrlPattern, 4, "Delete")]
        public async Task<ActionResult> Delete(string repo, string digest)
        {
            var blob = await _recordStore.GetBlobByDigestAsync(repo, digest);
            if (blob == null)
            {
                _logger.LogDebug("Blob not found {@blobQuery}...", new {repo, digest});
                return NotFound();
            }

            await _recordStore.DeleteBlobAsync(blob);
            await _blobStorage.DeleteAsync(blob.StorageLocation);
            
            _logger.LogInformation("Blob deleted: {@blobQuery}...", new {repo, digest});
            return Accepted();
        }


        private async Task<IActionResult> FinishUploading(string repo, string digest, FileInfo blobTempFile, bool chunkedEncoding, string sessionId)
        {
            _logger.LogDebug("Finishing upload for {@upload}...", new {repo, sessionId, digest});
            var chunkSaveSucceeded = await SaveChunk(blobTempFile, chunkedEncoding, true, repo, sessionId, digest);
            if (!chunkSaveSucceeded.Item1)
            {
                return chunkSaveSucceeded.Item2;
            }

            // refresh disk status
            blobTempFile = new FileInfo(blobTempFile.FullName);
            Digest requestedDigest = null;
            if (!blobTempFile.Exists || (!string.IsNullOrEmpty(digest) && !Digest.TryParse(digest, out requestedDigest)))
            {
                // If we are closing the upload and we can't find the stored temporary file, there must be something wrong.
                _logger.LogDebug("Can not finish upload for session {@session}", new {repo, sessionId, digest});
                return BadRequest();
            }

            await using var fileReceived = System.IO.File.OpenRead(blobTempFile.FullName);
            var computedDigest = Digest.Compute(fileReceived);
            var computedDigestString = computedDigest.ToString();
            await fileReceived.DisposeAsync();
            if (requestedDigest != null && !requestedDigest.Equals(computedDigest))
            {
                _logger.LogDebug("Digest did not match in upload {@upload}", new {repo, sessionId, digest, computedDigest = computedDigestString});
                blobTempFile.Delete();
                return BadRequest();
            }

            _logger.LogDebug("Saving new blob content from upload {@upload}...", new {repo, sessionId, digest});
            var existingLocation = await _blobStorage.TryLocateExistingAsync(computedDigestString);
            var savedLocation = existingLocation ?? (await _blobStorage.SaveAsync(blobTempFile, repo, computedDigestString));
            blobTempFile.Delete();

            var mediaType = Request.Headers["Content-Type"].ToString();
            var blobRecord = new BlobRecord
            {
                RepositoryName = repo,
                DigestString = computedDigestString,
                StorageLocation = savedLocation,
                ContentLength = blobTempFile.Length,
                MediaType = string.IsNullOrEmpty(mediaType) ? null : mediaType
            };
            await _recordStore.CreateBlobAsync(blobRecord);

            _logger.LogInformation("New blob saved {@upload}", new {repo, sessionId, digest, savedLocation, size = blobTempFile.Length});
            Response.Headers.Add("Docker-Content-Digest", computedDigestString);
            return Created($"/v2/{repo}/blobs/{computedDigestString}", null);
        }

        IActionResult StartUploading(string repo, string sessionId)
        {
            _logger.LogInformation("New uploading session created: {@session}...", new { repo, sessionId});
            Response.Headers.Add("Docker-Upload-UUID", sessionId);
            return Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
        }

        async Task<IActionResult> MountBlob(string repo, string digest, string @from, string sessionId)
        {
            if (!from.Contains('/'))
            {
                from = $"library/{from}";
            }
            var existedBlob = await _recordStore.GetBlobByDigestAsync(@from, digest);
            if (existedBlob == null)
            {
                _logger.LogDebug("Failed to mount blob from {@mount}", new {repo, digest, from});
                return StartUploading(repo, sessionId);
            }

            var blobRecord = new BlobRecord
            {
                RepositoryName = repo,
                DigestString = digest,
                StorageLocation = existedBlob.StorageLocation,
                ContentLength = existedBlob.ContentLength,
                MediaType = existedBlob.MediaType
            };
            await _recordStore.CreateBlobAsync(blobRecord);

            _logger.LogInformation("Blob mounted: from {@source} to: {@dest}",new {repo = existedBlob.RepositoryName, digest}, new {repo, digest});
            Response.Headers.Add("Docker-Content-Digest", digest);
            return Created($"/v2/{repo}/blobs/{digest}", null);
        }

        private async Task<Tuple<bool, IActionResult>> SaveChunk(FileInfo tempFile, bool chunkedEncoding, bool closing, 
            string repo, string sessionId, string digest)
        {
            var requestContentLength = Request.Headers.ContentLength;
            if (requestContentLength == 0)
            {
                return Tuple.Create(true, (IActionResult)null);
            }

            var existingLength = tempFile.Exists ? tempFile.Length : 0;
            if (!chunkedEncoding && !ValidateContentRange(Request.Headers["Content-Range"].ToString(), requestContentLength, existingLength, out var actionResult))
            {
                _logger.LogDebug("Bad request range header in session {@session}", new { repo, sessionId, digest});
                return Tuple.Create(false, actionResult);
            }

            if (!tempFile.Directory!.Exists)
            {
                tempFile.Directory.Create();
            }

            await using var receivedStream = tempFile.Exists ? tempFile.OpenWrite() : tempFile.Create();
            await Request.Body.CopyToAsync(receivedStream);

            var updatedLength = receivedStream.Length;
            var receivedLength = updatedLength - existingLength;
            if (receivedLength == 0 || (!chunkedEncoding && requestContentLength.HasValue && requestContentLength != receivedLength))
            {
                _logger.LogDebug("Content length did not match in session {@session}", new { repo, sessionId, digest});
                await receivedStream.DisposeAsync();
                actionResult = BadRequest();
                return Tuple.Create(false, actionResult);
            }

            if (!closing)
            {
                Response.Headers.Add("Range", $"{existingLength}-{updatedLength - 1}");
            }
            return Tuple.Create(true, (IActionResult)null);
        }

        private bool ValidateContentRange(string requestContentRange, long? requestContentLength, long existingLength, out IActionResult actionResult)
        {
            actionResult = null;
            if (string.IsNullOrEmpty(requestContentRange))
            {
                return true;
            }

            var regex = new Regex(@"^(bytes\s)?([\d]+)-([\d]+)(/.*)?$", RegexOptions.Compiled);
            var rangeMatch = regex.Match(requestContentRange);
            if (!rangeMatch.Success)
            {
                actionResult = BadRequest();
                return false;
            }

            var rangeStart = long.Parse(rangeMatch.Groups[2].Value);
            var rangeEnd = long.Parse(rangeMatch.Groups[3].Value);
            if (rangeStart != existingLength || requestContentLength != (rangeEnd - rangeStart + 1))
            {
                Response.Headers.Add("Range", $"0-{existingLength - 1}");
                actionResult = new StatusCodeResult((int) HttpStatusCode.RequestedRangeNotSatisfiable);
                return false;
            }

            return true;
        }

        private string TempPathForUploadingBlob(string sessionId)
        {
            return Path.Combine(_settings.TemporaryFilesRootPath, "uploading-blobs", sessionId);
        }
    }
    
    // todo:
    // 1. delete an ongoing upload
    // 2. get on ongoing upload (for resuming)
}

/*
 * Docker client does add Content-Length, but it seems was overridden by the Go net/http package and a 'Transfer-Encoding: chunked' header was added 
 * https://github.com/moby/moby/blob/797b974cb9/vendor/github.com/docker/distribution/registry/client/blob_writer.go#L72
 * https://golang.org/src/net/http/transfer.go#L167
 * 
 */
