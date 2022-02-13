using System;
using Microsoft.Extensions.Options;

namespace AzureUtils.AzureBlobProxy.Services;

internal sealed class ContainerValidator : IContainerValidator
{
    private readonly ContainerFilter _containerFilter;

    public ContainerValidator(IOptions<ContainerFilter> containerFilter)
    {
        if(containerFilter.Value.Regex == null)
            throw new ArgumentException("Filter should not be empty", nameof(containerFilter));

        _containerFilter = containerFilter.Value;
    }

    public bool IsValidContainerName(string containerName) =>
        _containerFilter.Regex.IsMatch(containerName);
}