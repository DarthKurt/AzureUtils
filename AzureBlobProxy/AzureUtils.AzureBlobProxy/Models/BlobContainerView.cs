using Azure.Storage.Blobs.Models;

namespace AzureUtils.AzureBlobProxy.Models;

public sealed record BlobContainerView(BlobItem[] Files, string[] Folders, string Container, string Path);