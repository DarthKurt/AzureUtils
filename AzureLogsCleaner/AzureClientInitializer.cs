﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

namespace AzureLogsCleaner;

internal sealed class AzureClientInitializer
{
    private readonly StorageManagementClient _storageManagementClient;
    private readonly ResourcesManagementClient _resourcesManagementClient;

    public AzureClientInitializer(
        StorageManagementClient storageManagementClient,
        ResourcesManagementClient resourcesManagementClient)
    {
        _storageManagementClient = storageManagementClient;
        _resourcesManagementClient = resourcesManagementClient;
    }

    public async Task<StorageAccountAnalyzer> CreateAnalyzerAsync()
    {
        var storageAccount = await SelectAccountAsync(_storageManagementClient).ConfigureAwait(false);
        var resourceGroup = await SelectResourceGroupAsync(_resourcesManagementClient).ConfigureAwait(false);

        return new StorageAccountAnalyzer(storageAccount, resourceGroup, _storageManagementClient);
    }

    private static async Task<StorageAccount> SelectAccountAsync(StorageManagementClient smClient)
    {
        Console.WriteLine("Available accounts:");
        var storageAccounts = new ConcurrentBag<StorageAccount>();
        await foreach (var item in smClient.StorageAccounts.ListAsync().ConfigureAwait(false))
        {
            Console.WriteLine($"{item.Name}");
            storageAccounts.Add(item);
        }

        Console.WriteLine("Select account:");
        var id = Console.ReadLine();
        while (!storageAccounts.Any(a => a.Name.Equals(id)))
        {
            Console.WriteLine($"Invalid account name: {id}.");
            Console.WriteLine("Select account:");
            id = Console.ReadLine();
        }

        var storageAccount = storageAccounts.First(a => a.Name.Equals(id));

        Console.WriteLine($"Selected account: {storageAccount.Name}.");
        return storageAccount;
    }

    private static async Task<ResourceGroup> SelectResourceGroupAsync(ResourcesManagementClient rmClient)
    {
        Console.WriteLine("Available groups:");
        var storageAccounts = new ConcurrentBag<ResourceGroup>();
        await foreach (var item in rmClient.ResourceGroups.ListAsync().ConfigureAwait(false))
        {
            Console.WriteLine($"{item.Name}");
            storageAccounts.Add(item);
        }

        Console.WriteLine("Select resource group:");
        var name = Console.ReadLine();
        while (!storageAccounts.Any(a => a.Name.Equals(name)))
        {
            Console.WriteLine($"Invalid resource group name: {name}.");
            Console.WriteLine("Select resource group:");
            name = Console.ReadLine();
        }

        var resourceGroup = storageAccounts.First(a => a.Name.Equals(name));

        Console.WriteLine($"Selected resource group: {resourceGroup.Name}.");
        return resourceGroup;
    }
}