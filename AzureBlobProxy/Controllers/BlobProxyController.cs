using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Mvc;

namespace AzureBlobProxy.Controllers;

[Route("[controller]")]
public sealed class BlobProxyController : Controller
{
    private readonly BlobServiceClient _blobServiceClient;
  
    public BlobProxyController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    [HttpGet("{container}")]
    public async Task<IActionResult> Index([FromRoute] string container, [FromQuery] string path, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new InvalidOperationException("Container name was not specified.");

        var client = _blobServiceClient.GetBlobContainerClient(container);

        var exists = await client.ExistsAsync(cancellation);

        if(!exists)
            throw new InvalidOperationException($"Container '{container}' is not exists.");

        var prefix = string.IsNullOrWhiteSpace(path) ? string.Empty : $"{path}/";

        var items = await GetFolders(client, prefix, cancellation)
	        .ToArrayAsync(cancellation);

        var folders = items
	        .Where(i => i.IsPrefix)
	        .Select(i => string.IsNullOrWhiteSpace(path) ? i.Prefix : i.Prefix[(path.Length + 1)..])
	        .Select(i =>  i.Trim('/'))
	        .ToArray();

        var files = items
	        .Where(i => i.IsBlob && !i.Blob.Deleted)
	        .Select(i => i.Blob)
	        .ToArray();

        var model = new BlobIndexModel(files, folders, container, path);

        return View("~/Pages/BlobProxy/Index.cshtml", model);
    }

    [HttpGet("file/{container}")]
    public async Task<IActionResult> File([FromRoute] string container, [FromQuery] string path, CancellationToken cancellation)
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

    private static async IAsyncEnumerable<BlobHierarchyItem> GetFolders(
	    BlobContainerClient client,
		string prefix,
	    [EnumeratorCancellation] CancellationToken cancellation)
    {
	    await foreach (var page in client
		                   .GetBlobsByHierarchyAsync(prefix: prefix,  delimiter: "/", cancellationToken: cancellation)
		                   .AsPages()
		                   .WithCancellation(cancellation))
	    {
		    foreach (var item in page.Values)
		    {
			    yield return item;
		    }
	    }
    }
}