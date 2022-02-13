using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureUtils.AzureBlobProxy.Controllers;

[Route("[controller]")]
[Authorize]
public sealed class FileController : Controller
{
    private readonly BlobServiceClient _blobServiceClient;
  
    public FileController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    [HttpGet]
    public async Task<IActionResult> File([FromQuery] string container, [FromQuery] string path, CancellationToken cancellation)
    {
	    if (string.IsNullOrWhiteSpace(container))
		    throw new InvalidOperationException("Container name was not specified.");

	    if (string.IsNullOrWhiteSpace(path))
		    throw new InvalidOperationException("File was not specified.");

	    var client = _blobServiceClient.GetBlobContainerClient(container);

	    var exists = await client.ExistsAsync(cancellation);

	    if(!exists)
		    throw new InvalidOperationException($"Container '{container}' is not exists.");

	    var blob = await client
		    .GetBlobsAsync(prefix: path, cancellationToken: cancellation)
		    .FirstOrDefaultAsync(cancellation);

	    if (blob == null)
		    return NotFound();

	    var blockBlob = client.GetBlockBlobClient(blob.Name);

	    return new BlobStorageResult(blockBlob, blob.Properties.ContentType);
    }
}