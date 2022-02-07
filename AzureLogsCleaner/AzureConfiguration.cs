namespace AzureLogsCleaner;

internal sealed record AzureConfiguration
{
    public string Subscription { get; set; }

    public string Tenant { get; set; }

    public string AppId { get; set; }
}