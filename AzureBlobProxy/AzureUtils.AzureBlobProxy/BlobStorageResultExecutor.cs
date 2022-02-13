using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Specialized;
using AzureUtils.AzureBlobProxy.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace AzureUtils.AzureBlobProxy;

internal sealed class BlobStorageResultExecutor : FileResultExecutorBase, IActionResultExecutor<BlobStorageResult>
{
    public BlobStorageResultExecutor(ILoggerFactory loggerFactory)
        : base(CreateLogger<BlobStorageResultExecutor>(loggerFactory))
    {
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(ActionContext context, BlobStorageResult result)
    {
        var cancellationToken = context.HttpContext.RequestAborted;
        var blobClient = result.Blob;
        Logger.ExecutingBlobStorageResult(result);

        if (HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            // Get the properties of the blob
            await GetBlobPropertiesAsync(context, result, blobClient, cancellationToken);
        }
        else
        {
            var (range, rangeLength, serveBody) =
                await GetBlobPropertiesAsync(context, result, blobClient, cancellationToken);

            if (!serveBody)
                return;

            // Download the blob in the specified range
            var hr = range is not null ? new HttpRange(range.From!.Value, rangeLength) : default;
            var response = await blobClient.DownloadStreamingAsync(hr, cancellationToken: cancellationToken);

            // if LastModified and ETag are not set, pull them from streaming result
            var bdr = response.Value;
            var details = bdr.Details;
            if (result.LastModified is null || result.EntityTag is null)
            {
                var httpResponseHeaders = context.HttpContext.Response.GetTypedHeaders();
                httpResponseHeaders.LastModified = result.LastModified ??= details.LastModified;
                httpResponseHeaders.ETag = result.EntityTag ??= MakeEtag(details.ETag);
            }

            var stream = bdr.Content;
            await using (stream)
            {
                await WriteAsync(context, stream);
            }
        }
    }

    private async Task<(RangeItemHeaderValue? range, long rangeLength, bool serveBody)> GetBlobPropertiesAsync(
        ActionContext context, FileResult result, BlobBaseClient blobClient, CancellationToken cancellationToken)
    {
        var propertiesResponse = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        var properties = propertiesResponse.Value;

        result.LastModified ??= properties.LastModified;
        result.EntityTag ??= MakeEtag(properties.ETag);

        return SetHeadersAndLog(
            context,
            result,
            properties.ContentLength,
            result.EnableRangeProcessing,
            result.LastModified,
            result.EntityTag);
    }

    /// <summary>
    /// Write the contents of the <see cref="BlobStorageResult"/> to the response body.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="stream">The <see cref="Stream"/> to write.</param>
    private static Task WriteAsync(ActionContext context, Stream stream)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        return WriteFileAsync(context.HttpContext,
            stream,
            null, // prevent seeking
            0);
    }

    private static EntityTagHeaderValue MakeEtag(ETag eTag) => new(eTag.ToString("H"));
}