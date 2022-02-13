using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AzureUtils.AzureBlobProxy;

/// <summary>
/// Represents an <see cref="ActionResult"/> that when executed
/// will write a file from a blob to the response.
/// </summary>
internal sealed class BlobStorageResult : FileResult
{
    public BlobStorageResult(BlockBlobClient blob, string contentType) : base(contentType)
    {
        Blob = blob;
    }

    /// <summary>
    /// Gets the URL for the block blob to be returned.
    /// </summary>
    public BlockBlobClient Blob { get; }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var executor = context.HttpContext.RequestServices
            .GetRequiredService<IActionResultExecutor<BlobStorageResult>>();

        return executor.ExecuteAsync(context, this);
    }
}
