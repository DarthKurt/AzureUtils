using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

namespace AzureLogsCleaner
{
    public class StorageAccountAnalyzer
    {
        private readonly StorageAccount _account;
        private readonly ResourceGroup _resourceGroup;
        private readonly StorageManagementClient _smClient;

        public StorageAccountAnalyzer(
            StorageAccount account,
            ResourceGroup resourceGroup,
            StorageManagementClient smClient)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _resourceGroup = resourceGroup ?? throw new ArgumentNullException(nameof(resourceGroup));
            _smClient = smClient ?? throw new ArgumentNullException(nameof(smClient));
        }

        public async Task<int> DoAsync(DateTime threshold)
        {
            Console.WriteLine("Tables:");
            var tables = new ConcurrentBag<Table>();
            await foreach (var item in _smClient.Table.ListAsync(_resourceGroup.Name, _account.Name).ConfigureAwait(false))
            {
                Console.WriteLine($"{item.Name}");
                tables.Add(item);
            }

            Console.WriteLine("Select table:");
            var name = Console.ReadLine();
            while (!tables.Any(a => a.Name.Equals(name)))
            {
                Console.WriteLine($"Invalid table name: {name}.");
                Console.WriteLine("Select table:");
                name = Console.ReadLine();
            }

            var table = tables.FirstOrDefault(a => a.Name.Equals(name));
            if (table == null)
                return 0;

            Console.WriteLine($"Selected table: {table.Name}.");

            var tableServiceClient = await CreateTableServiceClientAsync(table).ConfigureAwait(false);
            var tableClient = tableServiceClient.GetTableClient(table.TableName);
            var res = await GetRecordsAsync(tableClient, threshold).ConfigureAwait(false);

            return res.Count();
        }

        private async Task<TableServiceClient> CreateTableServiceClientAsync(Table table)
        {
            var storageAccountKey = await _smClient.StorageAccounts
                .ListKeysAsync(_resourceGroup.Name, _account.Name)
                .ConfigureAwait(false);
            var firstKey = storageAccountKey?.Value?.Keys?.First();

            // TODO: Find out correct way to create client by name
            var uriBuilder = new UriBuilder("https", $"{_account.Name}.table.core.windows.net", 443, table.TableName);

            var tableServiceClient = new TableServiceClient(uriBuilder.Uri,
                new TableSharedKeyCredential(_account.Name, firstKey?.Value));
            return tableServiceClient;
        }

        private static async Task<IEnumerable<ITableEntity>> GetRecordsAsync(TableClient client, DateTime threshold)
        {
            // BUG: Here we get 501 - need to find out why?
            var traceLogsTableQuery = client.QueryAsync<EventLogEntry>(
                e => string.Compare(e.PartitionKey, "0" + threshold.Ticks, StringComparison.Ordinal) >= 0,
                select: new []{ "PartitionKey", "RowKey" })
                .ConfigureAwait(false);

            var res = new ConcurrentBag<ITableEntity>();

            await foreach (var traceLog in traceLogsTableQuery)
            {
                res.Add(traceLog);
            }

            return res;
        }

        private class EventLogEntry : ITableEntity
        {
            public int EventId { get; set; }

            public string PartitionKey { get; set; }

            public string RowKey { get; set; }

            public DateTimeOffset? Timestamp { get; set; }

            public ETag ETag { get; set; }

            public string Description { get; set; }

            private DateTime PreciseTimeStamp { get; set; }
        }
    }
}
