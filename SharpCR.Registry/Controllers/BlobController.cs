using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharpCR.Registry.Models;
using SharpCR.Registry.Records;

namespace SharpCR.Registry.Controllers
{
    public class BlobController : ControllerBase
    {
        private readonly IRecordStore _recordStore;
        private readonly IBlobStorage _blobStorage;
        private readonly Settings _settings;

        public BlobController(IRecordStore recordStore, IBlobStorage blobStorage, IOptions<Settings> settings, IWebHostEnvironment environment)
        {
            _recordStore = recordStore;
            _blobStorage = blobStorage;
            _settings = settings.Value;
            _settings.TemporaryFilesRootPath ??= environment.ContentRootPath;
        }

        [RegistryRoute("blobs/{digest}")]
        [HttpGet]
        [HttpHead]
        public async Task<IActionResult> Get(string repo, string digest)
        {
            var blob = await _recordStore.GetBlobByDigestAsync(repo, digest);
            if (blob == null)
            {
                return NotFound();
            }

            HttpContext.Response.Headers.Add("Docker-Content-Digest", blob.DigestString);
            HttpContext.Response.Headers.Add("Content-Length", blob.ContentLength.ToString());
            var writeFile = string.Equals(HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (!writeFile)
            {
                return new EmptyResult();
            }

            if (!_blobStorage.SupportsDownloading)
            {
                var content = await _blobStorage.ReadAsync(blob.StorageLocation);
                return new FileStreamResult(content, blob.MediaType ?? "application/octet-stream");
            }
            else
            {
                var downloadableUrl = await _blobStorage.GenerateDownloadUrlAsync(blob.StorageLocation);
                return Redirect(downloadableUrl);
            }
        }

        [RegistryRoute("blobs/uploads")]
        [HttpPost]
        public async Task<IActionResult> CreateUpload(string repo, [FromQuery] string digest, [FromQuery] string mount, [FromQuery] string @from)
        {
            var sessionId = $"{_settings.BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";
            var monolithicUpload = !string.IsNullOrEmpty(digest);
            var isMount = !string.IsNullOrEmpty(mount);
            var chunkedEncoding = !Request.ContentLength.HasValue && Request.Headers["Transfer-Encoding"] == "chunked";

            if (monolithicUpload)
            {
                return await FinishUploading(repo, digest, new FileInfo(TempPathForUploadingBlob(sessionId)), chunkedEncoding);
            }
            else if (isMount)
            {
                return await MountBlob(repo, mount, @from, sessionId);
            }
            else
            {
                return StartUploading(repo, sessionId);
            }
        }

        [RegistryRoute("blobs/uploads/{sessionId}")]
        [HttpPatch]
        [HttpPut]
        public async Task<IActionResult> ContinueUpload(string repo, string sessionId, [FromQuery] string digest)
        {
            var sessionIdPrefix = sessionId.Split("_", StringSplitOptions.RemoveEmptyEntries);
            if (sessionIdPrefix.Length != 2 || !string.Equals(sessionIdPrefix[0], _settings.BlobUploadSessionIdPrefix))
            {
                return BadRequest();
            }

            var blobTempFile = new FileInfo(TempPathForUploadingBlob(sessionId));
            var receivingChunks = string.Equals(Request.Method, HttpMethod.Patch.ToString(), StringComparison.OrdinalIgnoreCase);
            var chunkedEncoding = !Request.ContentLength.HasValue && Request.Headers["Transfer-Encoding"] == "chunked";

            if (receivingChunks)
            {
                Response.Headers.Add("Docker-Upload-UUID", sessionId);
                var chunkSaveSucceeded = await SaveChunk(blobTempFile, chunkedEncoding, false);
                return chunkSaveSucceeded.Item1 
                    ? Accepted($"/v2/{repo}/blobs/uploads/{sessionId}")
                    : chunkSaveSucceeded.Item2;
            }

            return await FinishUploading(repo, digest, blobTempFile, chunkedEncoding);
        }

        [RegistryRoute("blobs/{digest}")]
        [HttpDelete]
        public async Task<ActionResult> Delete(string repo, string digest)
        {
            var blob = await _recordStore.GetBlobByDigestAsync(repo, digest);
            if (blob == null)
            {
                return NotFound();
            }

            await _recordStore.DeleteBlobAsync(blob);
            await _blobStorage.DeleteAsync(blob.StorageLocation);
            return Accepted();
        }


        private async Task<IActionResult> FinishUploading(string repo, string digest, FileInfo blobTempFile, bool chunkedEncoding)
        {
            var chunkSaveSucceeded = await SaveChunk(blobTempFile, chunkedEncoding, true);
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
                return BadRequest();
            }

            await using var fileReceived = System.IO.File.OpenRead(blobTempFile.FullName);
            var computedDigest = Digest.Compute(fileReceived);
            var computedDigestString = computedDigest.ToString();
            if (requestedDigest != null && !requestedDigest.Equals(computedDigest))
            {
                blobTempFile.Delete();
                return BadRequest();
            }

            fileReceived.Seek(0, SeekOrigin.Begin);
            var savedLocation = await _blobStorage.SaveAsync(repo, computedDigestString, fileReceived);
            await fileReceived.DisposeAsync();
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

            Response.Headers.Add("Docker-Content-Digest", computedDigestString);
            return Created($"/v2/{repo}/blobs/{computedDigestString}", null);
        }

        IActionResult StartUploading(string repo, string sessionId)
        {
            Response.Headers.Add("Docker-Upload-UUID", sessionId);
            return Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
        }

        async Task<IActionResult> MountBlob(string repo, string digest, string @from, string sessionId)
        {
            var existedBlob = await _recordStore.GetBlobByDigestAsync(@from, digest);
            if (existedBlob == null)
            {
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

            Response.Headers.Add("Docker-Content-Digest", digest);
            return Created($"/v2/{repo}/blobs/{digest}", null);
        }

        private async Task<Tuple<bool, IActionResult>> SaveChunk(FileInfo tempFile, bool chunkedEncoding, bool closing)
        {
            var requestContentLength = Request.Headers.ContentLength;
            if (requestContentLength == 0)
            {
                return Tuple.Create(true, (IActionResult)null);
            }

            var existingLength = tempFile.Exists ? tempFile.Length : 0;
            if (!chunkedEncoding && !ValidateContentRange(Request.Headers["Content-Range"].ToString(), requestContentLength, existingLength, out var actionResult))
            {
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
}
