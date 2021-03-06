using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        public IActionResult Get(string repo, string digest)
        {
            var blob = _recordStore.GetBlobByDigest(repo, digest);
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
                var content = _blobStorage.Read(blob.StorageLocation);
                return new FileStreamResult(content, "application/octet-stream");
            }
            else
            {
                var downloadableUrl = _blobStorage.GenerateDownloadUrl(blob.StorageLocation);
                return Redirect(downloadableUrl);
            }
        }

        [RegistryRoute("blobs/uploads")]
        [HttpPost]
        public IActionResult CreateUpload(string repo, [FromQuery] string digest, [FromQuery] string mount, [FromQuery] string @from)
        {
            var sessionId = $"{_settings.BlobUploadSessionIdPrefix}_{Guid.NewGuid():N}";
            var singlePost = !string.IsNullOrEmpty(digest);
            var chunkedEncoding = !Request.ContentLength.HasValue && Request.Headers["Transfer-Encoding"] == "chunked";

            if (singlePost)
            {
                return FinishUploading(repo, digest, new FileInfo(TempPathForUploadingBlob(sessionId)),
                    chunkedEncoding);
            }
            else
            {
                Response.Headers.Add("Docker-Upload-UUID", sessionId);
                return Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
            }
        }

        [RegistryRoute("blobs/uploads/{sessionId}")]
        [HttpPatch]
        [HttpPut]
        public IActionResult ContinueUpload(string repo, string sessionId, [FromQuery] string digest)
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
                return !SaveChunk(blobTempFile, chunkedEncoding,  false, out var exceptionalResult) 
                    ? exceptionalResult 
                    : Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
            }
            
            return FinishUploading(repo, digest, blobTempFile, chunkedEncoding);
        }

        [RegistryRoute("blobs/{digest}")]
        [HttpDelete]
        public ActionResult Delete(string repo, string digest)
        {
            var blob = _recordStore.GetBlobByDigest(repo, digest);
            if (blob == null)
            {
                return NotFound();
            }

            _recordStore.DeleteBlob(blob);
            _blobStorage.Delete(blob.StorageLocation);
            return Accepted();
        }
        
        
        private IActionResult FinishUploading(string repo, string digest, FileInfo blobTempFile, bool chunkedEncoding)
        {
            if (!SaveChunk(blobTempFile, chunkedEncoding, true, out var exceptionalResult))
            {
                return exceptionalResult;
            }

            // refresh disk status
            blobTempFile = new FileInfo(blobTempFile.FullName);
            Digest requestedDigest = null;
            if (!blobTempFile.Exists || (!string.IsNullOrEmpty(digest) && !Digest.TryParse(digest, out requestedDigest)))
            {
                // If we are closing the upload and we can't find the stored temporary file, there must be something wrong.
                return BadRequest();
            }
            
            using var fileReceived = System.IO.File.OpenRead(blobTempFile.FullName);
            var computedDigest = Digest.Compute(fileReceived);
            if (requestedDigest != null && !requestedDigest.Equals(computedDigest))
            {
                blobTempFile.Delete();
                return BadRequest();
            }

            var digestString = computedDigest.ToString();
            var savedLocation = _blobStorage.Save(repo, digestString, fileReceived);
            fileReceived.Dispose();
            blobTempFile.Delete();

            var blobRecord = new BlobRecord
            {
                RepositoryName = repo,
                DigestString = digestString,
                StorageLocation = savedLocation,
                ContentLength = blobTempFile.Length
            };
            _recordStore.CreateBlob(blobRecord);
            
            Response.Headers.Add("Docker-Content-Digest", digestString);
            return Created($"/v2/{repo}/blobs/{digestString}", null);
        }

        private bool SaveChunk(FileInfo tempFile, bool chunkedEncoding, bool closing, out IActionResult actionResult)
        {
            actionResult = null;
            var requestContentLength = Request.Headers.ContentLength;
            if (requestContentLength == 0)
            {
                return true;
            }

            var existingLength = tempFile.Exists ? tempFile.Length : 0;
            if (!chunkedEncoding && !ValidateContentRange(Request.Headers["Content-Range"].ToString(), requestContentLength, existingLength, out actionResult))
            {
                return false;
            }

            if (!tempFile.Directory!.Exists)
            {
                tempFile.Directory.Create();
            }
            using var receivedStream = tempFile.Exists ? tempFile.OpenWrite() : tempFile.Create();
            Request.Body.CopyTo(receivedStream);
            
            var updatedLength = receivedStream.Length;
            var receivedLength = updatedLength - existingLength;
            if (!closing && receivedLength > 0)
            {
                Response.Headers.Add("Range", $"{existingLength}-{updatedLength-1}");
            }

            if (!chunkedEncoding && requestContentLength != receivedLength)
            {
                receivedStream.Dispose();
                tempFile.Delete();
                actionResult = BadRequest();
                return false;
            }
            return true;
        }

        private bool ValidateContentRange(string requestContentRange,  long? requestContentLength, long existingLength, out IActionResult actionResult)
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
                Response.Headers.Add("Range", $"0-{existingLength-1}");
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
