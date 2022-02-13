using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureUtils.AzureBlobProxy.Models;
using AzureUtils.AzureBlobProxy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureUtils.AzureBlobProxy.Controllers;

[Route("[controller]")]
[Authorize]
public sealed class BlobController : Controller
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IContainerValidator _containerValidator;

    public BlobController(BlobServiceClient blobServiceClient, IContainerValidator containerValidator)
    {
	    _blobServiceClient = blobServiceClient;
	    _containerValidator = containerValidator;
    }

    [HttpGet]
    public async Task<IActionResult> ContainerIndex(CancellationToken cancellation)
    {
	    var containers = await GetContainers(_blobServiceClient, cancellation)
		    .ToArrayAsync(cancellation);

	    if(containers == null)
		    throw new InvalidOperationException($"No containers found.");

	    var folders = containers
		    .Where(i => !i.IsDeleted.HasValue || !i.IsDeleted.Value)
		    .Select(i => i.Name)
		    .Where(n => _containerValidator.IsValidContainerName(n))
		    .ToArray();

		var model = new BlobContainerRoot(folders);

	    return View("~/Views/Blob/ContainerIndex.cshtml" , model);
    }

    [HttpGet("{container}")]
    public async Task<IActionResult> ContainerView([FromRoute] string container, [FromQuery] string path, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new InvalidOperationException("Container name was not specified.");

        if (!_containerValidator.IsValidContainerName(container))
	        return StatusCode(StatusCodes.Status403Forbidden);

        var client = _blobServiceClient.GetBlobContainerClient(container);

        var exists = await client.ExistsAsync(cancellation);

        if(!exists)
            throw new InvalidOperationException($"Container '{container}' is not exists.");

        var prefix = string.IsNullOrWhiteSpace(path) ? string.Empty : $"{path}/";

        var items = await GetFolders(client, prefix, cancellation)
	        .ToArrayAsync(cancellation);

        var folders = items
	        .Where(i => i.IsPrefix)
	        .Select(i => FolderFactory(path, i.Prefix))
	        .ToArray();

        var files = items
	        .Where(i => i.IsBlob && !i.Blob.Deleted)
	        .Select(i => i.Blob)
	        .ToArray();

        var model = new BlobContainerView(files, folders, container, path);

        return View("~/Views/Blob/ContainerView.cshtml", model);
    }

    private static async IAsyncEnumerable<BlobContainerItem> GetContainers(
	    BlobServiceClient client, [EnumeratorCancellation] CancellationToken cancellation)
    {
	    await foreach (var page in client.GetBlobContainersAsync(cancellationToken: cancellation)
		                   .AsPages()
		                   .WithCancellation(cancellation))
	    {
		    foreach (var item in page.Values)
		    {
			    yield return item;
		    }
	    }
    }

    private static async IAsyncEnumerable<BlobHierarchyItem> GetFolders(
	    BlobContainerClient client, string prefix, [EnumeratorCancellation] CancellationToken cancellation)
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

    private static string FolderFactory(string? path, string prefix)
    {
	    prefix = string.IsNullOrWhiteSpace(path)
		    ? prefix
		    : prefix[(path.Length + 1)..];

	    return prefix.Trim('/');
    }
}