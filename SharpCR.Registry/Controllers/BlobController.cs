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
            _settings = settings.Value ?? new Settings {TemporaryFilesRootPath = environment.ContentRootPath };
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
            if (!singlePost)
            {
                return Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
            }
            
            return UploadWithDigest(repo, digest, new FileInfo(TempPathForUploadingBlob(sessionId)));
        }

        [RegistryRoute("blobs/uploads/{sessionId}")]
        [HttpPatch]
        [HttpPut]
        public IActionResult ContinueUpload(string repo, string sessionId, [FromQuery] string digest)
        {
            var blobTempFile = new FileInfo(TempPathForUploadingBlob(sessionId));
            var receivingChunks = string.Equals(Request.Method, HttpMethod.Patch.ToString(), StringComparison.OrdinalIgnoreCase);

            if (receivingChunks)
            {
                return !SaveChunk(blobTempFile, out var exceptionalResult) 
                    ? exceptionalResult 
                    : Accepted($"/v2/{repo}/blobs/uploads/{sessionId}");
            }

            return UploadWithDigest(repo, digest, blobTempFile);
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
        
        
        private IActionResult UploadWithDigest(string repo, string digest, FileInfo blobTempFile)
        {
            if (!SaveChunk(blobTempFile, out var exceptionalResult))
            {
                return exceptionalResult;
            }

            if (!Digest.TryParse(digest, out var requestedDigest) || !blobTempFile.Exists)
            {
                // If we are closing the upload and we can't find the stored temporary file, there must be something wrong.
                return BadRequest();
            }
            
            using var fileReceived = System.IO.File.OpenRead(blobTempFile.FullName);
            var computedDigest = Digest.Compute(fileReceived);
            if (!requestedDigest.Equals(computedDigest))
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
            return Created($"/v2/{repo}/blobs/{digestString}", null);
        }

        private bool SaveChunk(FileInfo tempFile, out IActionResult actionResult)
        {
            actionResult = null;
            var requestContentLength = Request.Headers.ContentLength;
            var hasChunk = requestContentLength > 0;
            if (!hasChunk) return true;
            
            var existingLength = tempFile.Exists ? tempFile.Length : 0;
            if (!ValidateContentRange(Request.Headers["Content-Range"].ToString(), requestContentLength, existingLength, out actionResult))
            {
                return false;
            }
                
            using var receivedStream = tempFile.Exists ? tempFile.OpenWrite() : tempFile.Create();
            Request.Body.CopyTo(receivedStream);

            if (requestContentLength != (new FileInfo(tempFile.FullName).Length - existingLength))
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
                
            var regex = new Regex(@"^bytes\s([\d]+)-([\d]+)(/.*)?$", RegexOptions.Compiled);
            var rangeMatch = regex.Match(requestContentRange);
            if (!rangeMatch.Success)
            {
                actionResult = BadRequest();
                return false;
            }

            var rangeStart = long.Parse(rangeMatch.Groups[1].Value);
            var rangeEnd = long.Parse(rangeMatch.Groups[2].Value);
            if (rangeStart != existingLength || requestContentLength != (rangeEnd - rangeStart + 1))
            {
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
