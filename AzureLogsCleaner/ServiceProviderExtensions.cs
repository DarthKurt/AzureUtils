using System;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AzureLogsCleaner;

internal static class ServiceProviderExtensions
{
    public static DeviceFlowAzureCredential DeviceFlowAzureCredentialFactory(this IServiceProvider sp)
    {
        var scopes = new[] {
            "https://management.azure.com/user_impersonation"
        };

        var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

        return new DeviceFlowAzureCredential(options, scopes);
    }

    public static StorageManagementClient StorageManagementClientFactory(this IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

        var credentials = sp.GetRequiredService<TokenCredential>();

        return new StorageManagementClient(options?.Value?.Subscription, credentials);
    }

    public static ResourcesManagementClient ResourcesManagementClientFactory(this IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

        var credentials = sp.GetRequiredService<TokenCredential>();

        return new ResourcesManagementClient(options?.Value?.Subscription, credentials);
    }
}