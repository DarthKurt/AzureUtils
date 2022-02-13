using System.Text.RegularExpressions;

namespace AzureUtils.AzureBlobProxy;

internal sealed record ContainerFilter(Regex Regex);
