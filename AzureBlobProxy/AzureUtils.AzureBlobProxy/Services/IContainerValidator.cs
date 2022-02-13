namespace AzureUtils.AzureBlobProxy.Services;

public interface IContainerValidator
{
    bool IsValidContainerName(string containerName);
}