using System;
using System.Globalization;
using Azure.Storage.Blobs.Models;

namespace AzureBlobProxy;

public static class BlobItemExtensions
{
    public static string FileSizeInKb(this BlobItem item)
    {
        var bytesSize = item.Properties.ContentLength ?? 0;
        return $"{Math.Ceiling((double)bytesSize / 1024)} KB";
    }

    public static string UpdatedTime(this BlobItem item)
        => item.Properties.LastModified?.UtcDateTime.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public static string FileName(this BlobItem item, string? prefix)
        => string.IsNullOrWhiteSpace(prefix) ? item.Name : item.Name[(prefix.Length + 1)..];
}