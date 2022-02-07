using System;
using Azure.Core;
using AzureLogsCleaner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configBuilder =>
    {
        configBuilder.AddJsonFile("appsettings.json", true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services
            .Configure<HostOptions>(x => x.ShutdownTimeout = TimeSpan.FromSeconds(30))
            .Configure<AzureConfiguration>(hostContext.Configuration.GetSection("AzureConfiguration"))
            .AddSingleton<TokenCredential, DeviceFlowAzureCredential>(sp => sp.DeviceFlowAzureCredentialFactory())
            .AddTransient<AzureClientInitializer>()
            .AddTransient(sp => sp.ResourcesManagementClientFactory())
            .AddTransient(sp => sp.StorageManagementClientFactory())
            .AddHostedService<Worker>();
    }).Build();

hostBuilder.RunAsync();