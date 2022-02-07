using Azure.Storage.Blobs.Models;

namespace AzureBlobProxy;

public sealed record BlobIndexModel(BlobItem[] Files, string[] Folders, string Container, string Path);