using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AzureLogsCleaner
{
    internal class Program
    {
        public static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile("appsettings.json", true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .Configure<HostOptions>(x => x.ShutdownTimeout = TimeSpan.FromSeconds(30))
                        .Configure<AzureConfiguration>(hostContext.Configuration.GetSection("AzureConfiguration"))
                        .AddSingleton<TokenCredential, DeviceFlowAzureCredential>(DeviceFlowAzureCredentialFactory)
                        .AddTransient<AzureClientInitializer>()
                        .AddTransient(ResourcesManagementClientFactory)
                        .AddTransient(StorageManagementClientFactory)
                        .AddHostedService<Worker>();
                });

        private static DeviceFlowAzureCredential DeviceFlowAzureCredentialFactory(IServiceProvider sp)
        {
            var scopes = new[] {
                "https://management.azure.com/user_impersonation"
            };

            var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

            return new DeviceFlowAzureCredential(options, scopes);
        }

        private static StorageManagementClient StorageManagementClientFactory(IServiceProvider sp)
        {
            var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

            var credentials = sp.GetRequiredService<TokenCredential>();

            return new StorageManagementClient(options?.Value?.Subscription, credentials);
        }

        private static ResourcesManagementClient ResourcesManagementClientFactory(IServiceProvider sp)
        {
            var options = sp.GetRequiredService<IOptions<AzureConfiguration>>();

            var credentials = sp.GetRequiredService<TokenCredential>();

            return new ResourcesManagementClient(options?.Value?.Subscription, credentials);
        }
    }
}
